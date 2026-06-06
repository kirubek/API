using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class RevokedToken : AuditableEntity
{
    public required string JwtId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public required string Reason { get; set; }
}
