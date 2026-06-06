using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class LeaveChoice : AuditableEntity
{
    public Guid AnnualLeaveRequestId { get; set; }
    public AnnualLeaveRequest AnnualLeaveRequest { get; set; } = null!;
    
    public int ChoiceNumber { get; set; } // 1-6
    
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    
    public int Days { get; set; }
    
    // For split leave, track which split this choice belongs to
    public int? SplitIndex { get; set; }
}
