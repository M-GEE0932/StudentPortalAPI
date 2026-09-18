using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

/// <summary>
/// Background service that periodically hard-deletes notifications that have been
/// soft-deleted (IsDeleted == true) and are older than the configured retention period
/// (default 90 days). This keeps the database size manageable while respecting the
/// soft-delete workflow.
/// Runs once every 24 hours.
/// </summary>
public class NotificationArchiveService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationArchiveService> _logger;
    private readonly TimeSpan _retentionPeriod;
    private readonly TimeSpan _checkInterval;

    public NotificationArchiveService(
        IServiceProvider serviceProvider,
        ILogger<NotificationArchiveService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Configurable via appsettings.json, defaults to 90 days
        var retentionDays = configuration.GetValue<int>("NotificationArchive:RetentionDays", 90);
        _retentionPeriod = TimeSpan.FromDays(retentionDays);

        var checkHours = configuration.GetValue<int>("NotificationArchive:CheckIntervalHours", 24);
        _checkInterval = TimeSpan.FromHours(Math.Max(1, checkHours));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "NotificationArchiveService started. Retention period: {Days} days, check interval: {Hours} hours",
            _retentionPeriod.TotalDays, _checkInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Only hard-delete notifications that were soft-deleted AND are older than retention period
                var cutoff = DateTime.UtcNow - _retentionPeriod;
                var deletedCount = await notificationService.DeleteOlderThanAsync(cutoff);

                if (deletedCount > 0)
                    _logger.LogInformation("Archived {Count} notifications older than {Cutoff}", deletedCount, cutoff);
                else
                    _logger.LogDebug("No notifications to archive (cutoff: {Cutoff})", cutoff);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationArchiveService. Will retry in {Hours} hours.", _checkInterval.TotalHours);
                // Wait before retrying to avoid tight error loops
                try { await Task.Delay(_checkInterval, stoppingToken); } catch { break; }
            }
        }

        _logger.LogInformation("NotificationArchiveService stopped");
    }
}
