using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class HandoverWorkStatus : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public string MfgPartNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string WorkCarriedOut { get; set; } = string.Empty;
    public string WorkToBeDone { get; set; } = string.Empty;
    public string OutstandingIssue { get; set; } = string.Empty;
    
    // Navigation properties
    public Handover Handover { get; set; } = null!;
}
