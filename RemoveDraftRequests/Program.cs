using BaseOps.Infrastructure.Data;
using BaseOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var optionsBuilder = new DbContextOptionsBuilder<BaseOpsDbContext>();
optionsBuilder.UseNpgsql(connectionString);

using var dbContext = new BaseOpsDbContext(optionsBuilder.Options);

// Get all Draft leave requests
var draftRequests = await dbContext.AnnualLeaveRequests
    .Include(r => r.User)
    .Where(r => r.Status == AnnualLeaveRequestStatus.Draft)
    .ToListAsync();

Console.WriteLine($"Found {draftRequests.Count} Draft leave requests:");

foreach (var request in draftRequests)
{
    Console.WriteLine($"  - User: {request.User?.EmployeeId} ({request.User?.FullName}), Year: {request.Year}, Created: {request.CreatedAt}");
}

// Remove all Draft requests
if (draftRequests.Count > 0)
{
    dbContext.AnnualLeaveRequests.RemoveRange(draftRequests);
    await dbContext.SaveChangesAsync();
    Console.WriteLine($"Removed {draftRequests.Count} Draft leave requests");
}
else
{
    Console.WriteLine("No Draft leave requests found");
}
