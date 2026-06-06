using BaseOps.Domain.Common;
using BaseOps.Domain.Enums;

namespace BaseOps.Domain.Entities;

public sealed class AnnualLeaveRequest : RowVersionEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    
    public RoleAtSubmission RoleAtSubmission { get; set; }
    
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;
    
    public Guid? HangarId { get; set; }
    public Hangar? Hangar { get; set; }
    
    public Guid? ShopId { get; set; }
    public Shop? Shop { get; set; }
    
    public Guid SubmittedToUserId { get; set; }
    public ApplicationUser SubmittedToUser { get; set; } = null!;
    
    public LeaveType? LeaveType { get; set; }
    
    public int Year { get; set; }
    
    // Leave Choices
    public ICollection<LeaveChoice> LeaveChoices { get; set; } = new List<LeaveChoice>();
    
    public AnnualLeaveRequestStatus Status { get; set; } = AnnualLeaveRequestStatus.Draft;
    
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }
    
    public string? RejectionReason { get; set; }
    
    // Navigation to approved plan entry (if approved)
    public AnnualLeavePlanEntry? ApprovedPlanEntry { get; set; }
}

public enum RoleAtSubmission
{
    Employee = 1,
    TeamLeader = 2,
    Manager = 3
}

public enum LeaveType
{
    Full = 1,
    Split = 2
}

public enum AnnualLeaveRequestStatus
{
    Draft = 1,
    Submitted = 2,
    Reviewed = 3,
    Approved = 4,
    Rejected = 5
}
