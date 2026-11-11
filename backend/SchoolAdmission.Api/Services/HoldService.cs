using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Exceptions;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public class HoldService
{
    private readonly AppDbContext _db;
    private readonly HoldOptions _options;

    public HoldService(AppDbContext db, HoldOptions options)
    {
        _db = db;
        _options = options;
    }

    // ---------- API 4: apply / hold a seat ----------
    public async Task<HoldCreatedDto> CreateAsync(string classId, string studentId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        // 1. Lock the class row. Other requests for the SAME class wait here,
        //    so seat counting + inserting happens one request at a time per class.
        var locked = await _db.Classes
            .FromSqlInterpolated($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
            .ToListAsync();
        var cls = locked.FirstOrDefault()
            ?? throw new NotFoundException("CLASS_NOT_FOUND", "Class does not exist.");

        var now = DateTime.UtcNow;

        // 2. Persist expiry for stale holds of this class (frees seats + lets the
        //    student re-apply even if the background worker hasn't run yet)
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE holds SET status = 'EXPIRED' WHERE class_id = {classId} AND status = 'HELD' AND expires_at <= {now}");

        // 3. Capacity check
        var used = await _db.Holds.CountAsync(h =>
            h.ClassId == classId &&
            (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed));

        if (used >= cls.TotalSeats)
            throw new ConflictException("NO_SEATS_AVAILABLE", "No seats are available in this class.");

        // 4. One active/confirmed application per student per class
        var duplicate = await _db.Holds.AnyAsync(h =>
            h.ClassId == classId && h.StudentId == studentId &&
            (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed));

        if (duplicate)
            throw new ConflictException("DUPLICATE_APPLICATION",
                "This student already has an active or confirmed application for this class.");

        // 5. Create the hold
        var hold = new Hold
        {
            Id = "h_" + Guid.NewGuid().ToString("N")[..10],
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(_options.DurationSeconds)
        };

        _db.Holds.Add(hold);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new HoldCreatedDto
        {
            HoldId = hold.Id,
            ClassId = hold.ClassId,
            StudentId = hold.StudentId,
            Status = hold.Status,
            ExpiresAt = hold.ExpiresAt
        };
    }

    // ---------- API 5: hold details ----------
    public async Task<HoldDetailsDto> GetAsync(string holdId)
    {
        var hold = await _db.Holds.AsNoTracking().FirstOrDefaultAsync(h => h.Id == holdId)
            ?? throw new NotFoundException("HOLD_NOT_FOUND", "Reservation does not exist.");

        return new HoldDetailsDto
        {
            HoldId = hold.Id,
            ClassId = hold.ClassId,
            Status = EffectiveStatus(hold, DateTime.UtcNow),
            ExpiresAt = hold.ExpiresAt
        };
    }

    // ---------- API 6: confirm (idempotent) ----------
    public async Task<HoldConfirmedDto> ConfirmAsync(string holdId, string studentId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        // Lock this hold row so two simultaneous confirms can't both proceed
        var hold = await LockHoldAsync(holdId);

        if (hold.StudentId != studentId)
            throw new ForbiddenException("NOT_YOUR_RESERVATION", "This reservation belongs to another student.");

        // Idempotent: already confirmed -> same success response, no second increment
        if (hold.Status == HoldStatus.Confirmed)
            return new HoldConfirmedDto { HoldId = hold.Id, Status = HoldStatus.Confirmed };

        if (hold.Status == HoldStatus.Cancelled)
            throw new ConflictException("HOLD_CANCELLED", "This reservation was cancelled.");

        var now = DateTime.UtcNow;

        if (hold.Status == HoldStatus.Expired || hold.ExpiresAt <= now)
        {
            if (hold.Status == HoldStatus.Held)
            {
                hold.Status = HoldStatus.Expired;
                await _db.SaveChangesAsync();
                await tx.CommitAsync(); // save the EXPIRED status before returning the error
            }
            throw new ConflictException("HOLD_EXPIRED", "This reservation has expired.");
        }

        hold.Status = HoldStatus.Confirmed;
        hold.ConfirmedAt = now;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new HoldConfirmedDto { HoldId = hold.Id, Status = HoldStatus.Confirmed };
    }

    // ---------- API 7: cancel ----------
    public async Task CancelAsync(string holdId, string studentId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var hold = await LockHoldAsync(holdId);

        if (hold.StudentId != studentId)
            throw new ForbiddenException("NOT_YOUR_RESERVATION", "This reservation belongs to another student.");

        if (hold.Status == HoldStatus.Cancelled)
            return; // already cancelled -> 204 again

        if (hold.Status == HoldStatus.Confirmed)
            throw new ConflictException("HOLD_ALREADY_CONFIRMED",
                "A confirmed admission cannot be cancelled here.");

        if (hold.Status == HoldStatus.Expired || hold.ExpiresAt <= DateTime.UtcNow)
        {
            if (hold.Status == HoldStatus.Held)
            {
                hold.Status = HoldStatus.Expired;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            throw new ConflictException("HOLD_EXPIRED", "This reservation has already expired.");
        }

        hold.Status = HoldStatus.Cancelled;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ---------- API 8: all applications of a student ----------
    public async Task<List<MyHoldDto>> GetMineAsync(string studentId)
    {
        var rows = await _db.Holds
            .AsNoTracking()
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new
            {
                h.Id, h.ClassId, ClassName = h.Class!.Name,
                h.Status, h.ExpiresAt
            })
            .ToListAsync();

        var now = DateTime.UtcNow;

        return rows.Select(r => new MyHoldDto
        {
            HoldId = r.Id,
            ClassId = r.ClassId,
            ClassName = r.ClassName,
            Status = (r.Status == HoldStatus.Held && r.ExpiresAt <= now) ? HoldStatus.Expired : r.Status,
            ExpiresAt = r.ExpiresAt
        }).ToList();
    }

    // ---------- helpers ----------
    private async Task<Hold> LockHoldAsync(string holdId)
    {
        var rows = await _db.Holds
            .FromSqlInterpolated($"SELECT * FROM holds WHERE id = {holdId} FOR UPDATE")
            .ToListAsync();

        return rows.FirstOrDefault()
            ?? throw new NotFoundException("HOLD_NOT_FOUND", "Reservation does not exist.");
    }

    // A HELD row past its expiry is reported as EXPIRED even before the worker updates it
    private static string EffectiveStatus(Hold h, DateTime now)
        => h.Status == HoldStatus.Held && h.ExpiresAt <= now ? HoldStatus.Expired : h.Status;
}