using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class HandoverAircraft : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public string AircraftType { get; set; } = string.Empty;
    public string AircraftRegistration { get; set; } = string.Empty;
    public string MaintenanceType { get; set; } = string.Empty;
    public DateTime MaintenanceStartTime { get; set; }
    public DateTime? MaintenanceEndTime { get; set; }
    
    // Navigation properties
    public Handover Handover { get; set; } = null!;
}
