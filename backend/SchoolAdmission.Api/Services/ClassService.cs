using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Exceptions;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public class ClassService
{
    private readonly AppDbContext _db;

    public ClassService(AppDbContext db) => _db = db;

    public async Task<ClassDto> CreateAsync(CreateClassRequest req)
    {
        var name = req.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new BadRequestException("INVALID_NAME", "Class name is required.");
        if (name.Length > 200)
            throw new BadRequestException("INVALID_NAME", "Class name must be 200 characters or fewer.");
        if (req.TotalSeats <= 0)
            throw new BadRequestException("INVALID_SEATS", "Total seats must be a positive number.");
        if (req.TotalSeats > 100000)
            throw new BadRequestException("INVALID_SEATS", "Total seats must be 100000 or fewer.");

        if (await _db.Classes.AnyAsync(c => c.Name == name))
            throw new ConflictException("CLASS_ALREADY_EXISTS", "A class with this name already exists.");

        var cls = new SchoolClass
        {
            Id = "c_" + Guid.NewGuid().ToString("N")[..10],
            Name = name,
            TotalSeats = req.TotalSeats,
            CreatedAt = DateTime.UtcNow
        };

        _db.Classes.Add(cls);
        await _db.SaveChangesAsync();

        return new ClassDto { Id = cls.Id, Name = cls.Name, TotalSeats = cls.TotalSeats };
    }

    public async Task<List<ClassStatsDto>> GetAllAsync()
    {
        var now = DateTime.UtcNow;

        return await _db.Classes
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new ClassStatsDto
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                HeldSeats = _db.Holds.Count(h =>
                    h.ClassId == c.Id && h.Status == HoldStatus.Held && h.ExpiresAt > now),
                ConfirmedSeats = _db.Holds.Count(h =>
                    h.ClassId == c.Id && h.Status == HoldStatus.Confirmed)
            })
            .ToListAsync();
    }

    public async Task<ClassStatsDto> GetByIdAsync(string classId)
    {
        var now = DateTime.UtcNow;

        var result = await _db.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new ClassStatsDto
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                HeldSeats = _db.Holds.Count(h =>
                    h.ClassId == c.Id && h.Status == HoldStatus.Held && h.ExpiresAt > now),
                ConfirmedSeats = _db.Holds.Count(h =>
                    h.ClassId == c.Id && h.Status == HoldStatus.Confirmed)
            })
            .FirstOrDefaultAsync();

        return result ?? throw new NotFoundException("CLASS_NOT_FOUND", "Class does not exist.");
    }
}