using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class AnnualLeavePlanEntry : RowVersionEntity
{
    public Guid AnnualLeavePlanId { get; set; }
    public AnnualLeavePlan AnnualLeavePlan { get; set; } = null!;
    
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    
    public Guid AnnualLeaveRequestId { get; set; }
    public AnnualLeaveRequest AnnualLeaveRequest { get; set; } = null!;
    
    public DateTimeOffset ApprovedStartDate { get; set; }
    public DateTimeOffset ApprovedEndDate { get; set; }
    
    public SourceChoice SourceChoice { get; set; }
    
    public int PriorityScore { get; set; }
    
    public bool IsManuallyAdjusted { get; set; } = false;
    
    public DateTimeOffset? ManuallyAdjustedAt { get; set; }
    public Guid? ManuallyAdjustedByUserId { get; set; }
    public ApplicationUser? ManuallyAdjustedByUser { get; set; }
    
    public string? AdjustmentReason { get; set; }
    
    // For split leave, track which split this is
    public int? SplitIndex { get; set; }
}

public enum SourceChoice
{
    Choice1 = 1,
    Choice2 = 2,
    Choice3 = 3,
    Choice4 = 4,
    Choice5 = 5,
    Choice6 = 6
}
