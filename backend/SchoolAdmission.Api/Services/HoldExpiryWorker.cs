using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;

namespace SchoolAdmission.Api.Services;

public class HoldExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<HoldExpiryWorker> _log;

    public HoldExpiryWorker(IServiceScopeFactory scopes, ILogger<HoldExpiryWorker> log)
    {
        _scopes = scopes;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;

                var count = await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE holds SET status = 'EXPIRED' WHERE status = 'HELD' AND expires_at <= {now}",
                    stoppingToken);

                if (count > 0)
                    _log.LogInformation("Expired {Count} reservation(s)", count);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _log.LogError(ex, "Expiry worker failed; will retry");
            }
        }
    }
}