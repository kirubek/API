using Microsoft.EntityFrameworkCore;
using BaseOps.Infrastructure.Data;

// Script to update leave request statuses to 'Approved' for all finalized leave plans
// Run this from the BaseOps.API project directory using: dotnet script UpdateFinalizedPlans.cs
// Or add this as a temporary method in Program.cs and call it during startup

var optionsBuilder = new DbContextOptionsBuilder<BaseOpsDbContext>();
optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=BaseOps_API;Username=postgres;Password=postgres");

using var dbContext = new BaseOpsDbContext(optionsBuilder.Options);

// Get all finalized plans
var finalizedPlans = await dbContext.AnnualLeavePlans
    .Where(p => p.Status == BaseOps.Domain.Entities.AnnualLeavePlanStatus.Finalized)
    .Include(p => p.Entries)
    .ToListAsync();

Console.WriteLine($"Found {finalizedPlans.Count} finalized plans");

var updatedCount = 0;

foreach (var plan in finalizedPlans)
{
    // Get all leave request IDs from this plan's entries
    var requestIds = plan.Entries.Select(e => e.AnnualLeaveRequestId).Distinct().ToList();
    
    // Get the leave requests
    var leaveRequests = await dbContext.AnnualLeaveRequests
        .Where(r => requestIds.Contains(r.Id) && r.Status != BaseOps.Domain.Entities.AnnualLeaveRequestStatus.Approved)
        .ToListAsync();
    
    foreach (var request in leaveRequests)
    {
        request.Status = BaseOps.Domain.Entities.AnnualLeaveRequestStatus.Approved;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedBy = plan.FinalizedByUserId;
        updatedCount++;
    }
    
    Console.WriteLine($"Plan {plan.Id}: Updated {leaveRequests.Count} leave requests");
}

await dbContext.SaveChangesAsync();

Console.WriteLine($"Total updated leave requests: {updatedCount}");
