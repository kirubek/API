using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class AceAttachment : AuditableEntity
{
    public new Guid Id { get; set; }

    public Guid ActivityId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string GeneratedFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string StoragePath { get; set; } = string.Empty;

    public Guid UploadedBy { get; set; }

    public DateTime UploadTimestamp { get; set; } = DateTime.UtcNow;
}
