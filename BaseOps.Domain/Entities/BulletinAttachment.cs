using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class BulletinAttachment : AuditableEntity
{
    public Guid Id { get; set; }
    
    public Guid BulletinId { get; set; }
    
    public string OriginalFileName { get; set; } = string.Empty;
    
    public string GeneratedFileName { get; set; } = string.Empty;
    
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public string StoragePath { get; set; } = string.Empty;
    
    public Guid UploadedBy { get; set; }
    
    public DateTime UploadTimestamp { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Bulletin Bulletin { get; set; } = null!;
    public ApplicationUser Uploader { get; set; } = null!;
}
