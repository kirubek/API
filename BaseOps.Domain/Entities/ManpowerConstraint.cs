using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class ManpowerConstraint : AuditableEntity
{
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;
    
    public Guid? HangarId { get; set; }
    public Hangar? Hangar { get; set; }
    
    public Guid? ShopId { get; set; }
    public Shop? Shop { get; set; }
    
    public int Year { get; set; }
    
    // Maximum percentage of employees that can be on leave on any given day
    public decimal MaxLeavePercentage { get; set; } = 0.3m; // Default 30%
    
    // Minimum percentage of employees that must be available on any given day
    public decimal MinCoveragePercentage { get; set; } = 0.6m; // Default 60%
    
    // Maximum number of employees that can be on leave on any given day (overrides percentage if set)
    public int? MaxLeaveCount { get; set; }
    
    // Minimum number of employees that must be available on any given day (overrides percentage if set)
    public int? MinCoverageCount { get; set; }
    
    // Specific periods with different constraints (e.g., holiday seasons)
    public ICollection<ManpowerConstraintPeriod> SpecialPeriods { get; set; } = new List<ManpowerConstraintPeriod>();
    
    public bool IsActive { get; set; } = true;
}

public sealed class ManpowerConstraintPeriod : AuditableEntity
{
    public Guid ManpowerConstraintId { get; set; }
    public ManpowerConstraint ManpowerConstraint { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    
    public decimal? MaxLeavePercentage { get; set; }
    public decimal? MinCoveragePercentage { get; set; }
    public int? MaxLeaveCount { get; set; }
    public int? MinCoverageCount { get; set; }
}
