using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class HandoverManningStatus : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public int TotalScheduledManpower { get; set; }
    public int SickLeave { get; set; }
    public int Absent { get; set; }
    public int Vacation { get; set; }
    public int Training { get; set; }
    public int BorrowedManpower { get; set; }
    public int TotalAvailableManpower { get; set; }
    public int TotalLostManpower { get; set; }
    public double AvailabilityPercentage { get; set; }
    
    // Navigation properties
    public Handover Handover { get; set; } = null!;
}
