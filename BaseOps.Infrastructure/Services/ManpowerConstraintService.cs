using BaseOps.Domain.Entities;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaseOps.Infrastructure.Services;

public class ManpowerConstraintService(BaseOpsDbContext dbContext) : IManpowerConstraintService
{
    public async Task<ManpowerConstraint> GetConstraintsAsync(Guid sectionId, Guid? hangarId, Guid? shopId, int year, CancellationToken cancellationToken = default)
    {
        // Try to get specific constraint for hangar/shop
        ManpowerConstraint? constraint = null;

        if (hangarId.HasValue)
        {
            constraint = await dbContext.ManpowerConstraints
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.HangarId == hangarId.Value && c.Year == year && c.IsActive, cancellationToken);
        }
        else if (shopId.HasValue)
        {
            constraint = await dbContext.ManpowerConstraints
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ShopId == shopId.Value && c.Year == year && c.IsActive, cancellationToken);
        }

        // Fall back to section-level constraint
        if (constraint == null)
        {
            constraint = await dbContext.ManpowerConstraints
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.SectionId == sectionId && c.Year == year && c.IsActive, cancellationToken);
        }

        // Fall back to default constraints
        if (constraint == null)
        {
            constraint = new ManpowerConstraint
            {
                Id = Guid.NewGuid(),
                SectionId = sectionId,
                HangarId = hangarId,
                ShopId = shopId,
                Year = year,
                MaxLeavePercentage = 0.3m, // Default 30%
                MinCoveragePercentage = 0.6m, // Default 60%
                IsActive = true
            };
        }

        return constraint;
    }

    public async Task<bool> ValidateConstraintAsync(DateOnly date, int currentOnLeave, Guid sectionId, Guid? hangarId, Guid? shopId, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var year = date.Year;
        var constraints = await GetConstraintsAsync(sectionId, hangarId, shopId, year, cancellationToken);

        // Get total employees for the scope
        var totalEmployees = await GetTotalEmployeesAsync(sectionId, hangarId, shopId, excludeUserId, cancellationToken);

        // Calculate max allowed and min required
        var maxAllowed = constraints.MaxLeaveCount ?? 
            (int)Math.Ceiling(totalEmployees * constraints.MaxLeavePercentage);
        
        var minRequired = constraints.MinCoverageCount ??
            (int)Math.Ceiling(totalEmployees * constraints.MinCoveragePercentage);

        // Check if current on leave exceeds max allowed
        if (currentOnLeave > maxAllowed)
        {
            return false;
        }

        // Check if available staff is below minimum required
        var available = totalEmployees - currentOnLeave;
        if (available < minRequired)
        {
            return false;
        }

        return true;
    }

    private async Task<int> GetTotalEmployeesAsync(Guid sectionId, Guid? hangarId, Guid? shopId, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsNoTracking().Where(u => u.IsActive && u.SectionId == sectionId);

        if (hangarId.HasValue)
        {
            query = query.Where(u => u.HangarId == hangarId.Value);
        }
        if (shopId.HasValue)
        {
            query = query.Where(u => u.ShopId == shopId.Value);
        }

        // Exclude the specified user (e.g., team leader) from the count
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }
}
