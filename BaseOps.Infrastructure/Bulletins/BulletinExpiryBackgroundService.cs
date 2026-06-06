using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BaseOps.Infrastructure.Bulletins;

public sealed class BulletinExpiryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BulletinExpiryBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public BulletinExpiryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<BulletinExpiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bulletin Expiry Background Service is starting.");

        // Run immediately on startup
        await ProcessExpiredBulletinsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await ProcessExpiredBulletinsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired bulletins");
            }
        }

        _logger.LogInformation("Bulletin Expiry Background Service is stopping.");
    }

    private async Task ProcessExpiredBulletinsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BaseOpsDbContext>();

        var expiredBulletins = await dbContext.Bulletins
            .Where(b => b.IsActive && b.ExpiryDate < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (expiredBulletins.Count == 0)
        {
            _logger.LogDebug("No expired bulletins found.");
            return;
        }

        _logger.LogInformation("Found {Count} expired bulletins to process.", expiredBulletins.Count);

        foreach (var bulletin in expiredBulletins)
        {
            bulletin.IsActive = false;
            bulletin.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Deactivated bulletin {BulletinId} (expired on {ExpiryDate})", 
                bulletin.Id, bulletin.ExpiryDate);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully deactivated {Count} expired bulletins.", expiredBulletins.Count);
    }
}
