using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class UserSession : AuditableEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public required string CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
}
