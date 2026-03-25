namespace ImageLayoutComposer.Services;

/// <summary>
/// Background service that evicts sessions older than <see cref="SessionTtl"/> every
/// <see cref="CleanupInterval"/>, deleting their source upload files from disk.
/// Runs for the lifetime of the application host.
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private static readonly TimeSpan SessionTtl      = TimeSpan.FromHours(1);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    private readonly IStorageService _storage;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(IStorageService storage, ILogger<SessionCleanupService> logger)
    {
        _storage = storage;
        _logger  = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CleanupInterval, stoppingToken);
            try
            {
                _storage.EvictExpiredSessions(SessionTtl);
                _logger.LogDebug("Session cleanup ran; TTL={Ttl}", SessionTtl);
            }
            catch (Exception ex)
            {
                // Log but don't crash the host — the next tick will retry.
                _logger.LogError(ex, "Session cleanup encountered an error.");
            }
        }
    }
}
