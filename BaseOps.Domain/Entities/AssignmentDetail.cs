using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class AssignmentDetail : AuditableEntity
{
    public Guid DailyAssignmentId { get; set; }
    public Guid EmployeeId { get; set; }
    public string AssignedAircraft { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    
    // Navigation properties
    public DailyAssignment DailyAssignment { get; set; } = null!;
    public ApplicationUser Employee { get; set; } = null!;
}
