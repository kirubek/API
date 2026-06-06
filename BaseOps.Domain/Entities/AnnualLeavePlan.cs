using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class AnnualLeavePlan : RowVersionEntity
{
    public AnnualLeavePlanLevel Level { get; set; }
    
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;
    
    public Guid? HangarId { get; set; }
    public Hangar? Hangar { get; set; }
    
    public Guid? ShopId { get; set; }
    public Shop? Shop { get; set; }
    
    public Guid? TeamLeaderId { get; set; }
    public ApplicationUser? TeamLeader { get; set; }
    
    public int Year { get; set; }
    
    public AnnualLeavePlanStatus Status { get; set; } = AnnualLeavePlanStatus.Draft;
    
    public DateTimeOffset? FinalizedAt { get; set; }
    public Guid? FinalizedByUserId { get; set; }
    public ApplicationUser? FinalizedByUser { get; set; }
    
    public ICollection<AnnualLeavePlanEntry> Entries { get; set; } = new List<AnnualLeavePlanEntry>();
    
    // Analytics snapshot at generation time
    public int TotalEmployees { get; set; }
    public int TotalOnLeave { get; set; }
    public int TotalAvailable { get; set; }
    
    public string? GenerationNotes { get; set; }
}

public enum AnnualLeavePlanLevel
{
    TeamLeader = 1,
    Manager = 2,
    Director = 3
}

public enum AnnualLeavePlanStatus
{
    Draft = 1,
    Finalized = 2
}
