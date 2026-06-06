using Microsoft.EntityFrameworkCore;
using BaseOps.Infrastructure.Data;
using BaseOps.Domain.Entities;

var optionsBuilder = new DbContextOptionsBuilder<BaseOpsDbContext>();
optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=baseops;Username=postgres;Password=postgres");

using var dbContext = new BaseOpsDbContext(optionsBuilder.Options);

Console.WriteLine("Starting cleanup of all leave data...");

// Get all plans with their entries
var allPlans = await dbContext.AnnualLeavePlans
    .Include(p => p.Entries)
    .ToListAsync();

Console.WriteLine($"Found {allPlans.Count} leave plans to remove.");

// Remove all plan entries first (they must be removed before plans due to foreign key)
var totalEntries = 0;
foreach (var plan in allPlans)
{
    totalEntries += plan.Entries.Count;
    dbContext.AnnualLeavePlanEntries.RemoveRange(plan.Entries);
}

Console.WriteLine($"Removing {totalEntries} plan entries...");
await dbContext.SaveChangesAsync();

// Remove all plans
dbContext.AnnualLeavePlans.RemoveRange(allPlans);
Console.WriteLine($"Removing {allPlans.Count} plans...");
await dbContext.SaveChangesAsync();

// Get all leave requests
var allRequests = await dbContext.AnnualLeaveRequests.ToListAsync();
Console.WriteLine($"Found {allRequests.Count} leave requests to remove.");

// Remove all requests
dbContext.AnnualLeaveRequests.RemoveRange(allRequests);
Console.WriteLine($"Removing {allRequests.Count} leave requests...");
await dbContext.SaveChangesAsync();

Console.WriteLine("✅ All leave data has been successfully removed from the database.");
Console.WriteLine($"Summary:");
Console.WriteLine($"  - Plans removed: {allPlans.Count}");
Console.WriteLine($"  - Plan entries removed: {totalEntries}");
Console.WriteLine($"  - Leave requests removed: {allRequests.Count}");
