using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BaseOps.Infrastructure.Handovers;

public sealed class HandoverAlertBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HandoverAlertBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public HandoverAlertBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<HandoverAlertBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Handover Alert Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForOverdueHandovers(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for overdue handovers");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Handover Alert Background Service stopped");
    }

    private async Task CheckForOverdueHandovers(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BaseOpsDbContext>();

        var sixteenHoursAgo = DateTime.UtcNow.AddHours(-16);

        var overdueHandovers = await dbContext.Handovers
            .Include(h => h.OutgoingTeamLeader)
            .Include(h => h.IncomingTeamLeader)
            .AsNoTracking()
            .Where(h => h.Status == HandoverStatus.Pending 
                      && h.SubmittedAt.HasValue 
                      && h.SubmittedAt.Value < sixteenHoursAgo)
            .ToListAsync(cancellationToken);

        if (overdueHandovers.Count > 0)
        {
            _logger.LogWarning("Found {Count} overdue pending handovers (>16 hours)", overdueHandovers.Count);

            foreach (var handover in overdueHandovers)
            {
                var hoursPending = (DateTime.UtcNow - handover.SubmittedAt!.Value).TotalHours;
                _logger.LogWarning(
                    "Overdue Handover ID: {HandoverId}, Date: {Date}, Section: {SectionId}, Hours Pending: {HoursPending:F1}, Outgoing TL: {OutgoingTL}, Incoming TL: {IncomingTL}",
                    handover.Id,
                    handover.Date,
                    handover.SectionId,
                    hoursPending,
                    handover.OutgoingTeamLeader?.FullName ?? "Unknown",
                    handover.IncomingTeamLeader?.FullName ?? "None"
                );

                // TODO: Integrate with notification system to alert managers
                // This could send emails, push notifications, or create alert records
                // For now, we log the information for manual monitoring
            }
        }
        else
        {
            _logger.LogDebug("No overdue pending handovers found");
        }
    }
}
