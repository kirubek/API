using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class OperationalRecord : AuditableEntity
{
    public required string Module { get; set; }
    public string? Resource { get; set; }
    public string? Action { get; set; }
    public required string PayloadJson { get; set; }
    public required string Status { get; set; }
}
