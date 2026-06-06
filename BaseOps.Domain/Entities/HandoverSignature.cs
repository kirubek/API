using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public enum SignatureRole
{
    Outgoing = 1,
    Incoming = 2,
    Employee = 3
}

public sealed class HandoverSignature : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public Guid UserId { get; set; }
    public SignatureRole SignatureRole { get; set; }
    public string SignatureData { get; set; } = string.Empty; // Base64 encoded signature image
    public string SignatureName { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    
    // Navigation properties
    public Handover Handover { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
