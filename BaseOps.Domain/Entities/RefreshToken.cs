using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class RefreshToken : AuditableEntity
{
    public required string TokenHash { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? IpAddress { get; set; }
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
