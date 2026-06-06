using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class HandoverDefect : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public string AircraftRegistration { get; set; } = string.Empty;
    public string DefectDescription { get; set; } = string.Empty;
    public string NonRoutineCardNumber { get; set; } = string.Empty;
    public DateTime DefectLoginTime { get; set; }
    public string ItemStatus { get; set; } = "Open"; // Open or Closed
    
    // Navigation properties
    public Handover Handover { get; set; } = null!;
}
