namespace BaseOps.Domain.Common;

public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public uint Version { get; set; }
}

public abstract class RowVersionEntity : AuditableEntity
{
    public new byte[] Version { get; set; } = Array.Empty<byte>();
}
