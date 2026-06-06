using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class AuditLog : AuditableEntity
{
    public Guid? UserId { get; set; }
    public required string Action { get; set; }
    public required string EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public required string CorrelationId { get; set; }
    public string? BeforeValues { get; set; }
    public string? AfterValues { get; set; }
    public bool IsAuthorizationFailure { get; set; }
}
