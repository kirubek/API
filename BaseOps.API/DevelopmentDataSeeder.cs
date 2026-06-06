using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Authentication;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BaseOps.API;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        Log.Information("Starting database seeder...");
        
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BaseOpsDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var adminPassword = configuration["DevelopmentSeed:AdminPassword"] ?? "Admin123!";

        // Check if database is ready and migrations are applied
        Log.Information("Checking database state...");
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (!canConnect)
        {
            Log.Warning("Database cannot be connected to, skipping seed data.");
            return;
        }

        // Always seed manpower constraints and leave balances for organizational users
        // Organizational data (Workspace, Sections, Director, Managers, Hangars, Shops) is seeded via migration
        await SeedManpowerConstraintsAndLeaveBalances(dbContext);
        Log.Information("Database seeder completed.");
    }

    private static async Task SeedManpowerConstraintsAndLeaveBalances(BaseOpsDbContext dbContext)
    {
        // Seed manpower constraints for annual leave
        Log.Information("Seeding manpower constraints for organizational users...");
        var currentYear = DateTime.UtcNow.Year;

        // Get all sections, hangars, and shops from organizational data
        var sections = await dbContext.Sections.ToListAsync();
        var hangars = await dbContext.Hangars.ToListAsync();
        var shops = await dbContext.Shops.ToListAsync();

        // Clear existing constraints for this year to avoid conflicts
        var existingConstraints = await dbContext.ManpowerConstraints
            .Where(c => c.Year == currentYear)
            .ToListAsync();
        if (existingConstraints.Any())
        {
            Log.Information($"Found {existingConstraints.Count} existing constraints for year {currentYear}, removing them...");
            dbContext.ManpowerConstraints.RemoveRange(existingConstraints);
            await dbContext.SaveChangesAsync();
        }

        // Section-level constraints
        foreach (var section in sections)
        {
            var sectionConstraint = new ManpowerConstraint
            {
                SectionId = section.Id,
                Year = currentYear,
                MaxLeavePercentage = 0.3m, // 30% max on leave
                MinCoveragePercentage = 0.6m, // 60% minimum coverage
                IsActive = true
            };
            dbContext.ManpowerConstraints.Add(sectionConstraint);
            await dbContext.SaveChangesAsync();
            Log.Information($"Created section-level manpower constraint for {section.Code}");
        }

        // Hangar-level constraints
        foreach (var hangar in hangars)
        {
            var hangarConstraint = new ManpowerConstraint
            {
                SectionId = hangar.SectionId,
                HangarId = hangar.Id,
                Year = currentYear,
                MaxLeavePercentage = 0.25m, // 25% max on leave (stricter for hangars)
                MinCoveragePercentage = 0.65m, // 65% minimum coverage
                IsActive = true
            };
            dbContext.ManpowerConstraints.Add(hangarConstraint);
            await dbContext.SaveChangesAsync();
            Log.Information($"Created hangar-level manpower constraint for {hangar.Code}");
        }

        // Shop-level constraints
        foreach (var shop in shops)
        {
            var shopConstraint = new ManpowerConstraint
            {
                SectionId = shop.SectionId,
                ShopId = shop.Id,
                Year = currentYear,
                MaxLeavePercentage = 0.2m, // 20% max on leave (stricter for shops)
                MinCoveragePercentage = 0.7m, // 70% minimum coverage
                IsActive = true
            };
            dbContext.ManpowerConstraints.Add(shopConstraint);
            await dbContext.SaveChangesAsync();
            Log.Information($"Created shop-level manpower constraint for {shop.Code}");
        }

        // Seed leave balances for all organizational users
        Log.Information("Seeding leave balances for organizational users...");
        var users = await dbContext.Users.ToListAsync();
        foreach (var user in users)
        {
            var existingBalance = await dbContext.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == user.Id && lb.Year == currentYear);
            if (existingBalance is null)
            {
                dbContext.LeaveBalances.Add(new LeaveBalance
                {
                    UserId = user.Id,
                    Year = currentYear,
                    TotalEntitled = 30,
                    Taken = 0,
                    Pending = 0,
                    CarryOverFromPrevious = 0,
                    CarryOverToNext = 0
                });
                await dbContext.SaveChangesAsync();
            }
        }
        await dbContext.SaveChangesAsync();
        Log.Information("Leave balances seeded for organizational users.");
    }
}
