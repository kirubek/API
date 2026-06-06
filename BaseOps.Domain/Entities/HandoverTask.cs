using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public enum TaskType
{
    Completed = 1,
    Outstanding = 2
}

public sealed class HandoverTask : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public TaskType TaskType { get; set; }
    public string? AircraftRegistration { get; set; }
    public string? TaskCardCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    
    // Navigation properties
    public Handover Handover { get; set; } = null!;
    public ApplicationUser? CreatedByUser { get; set; }
}
