using BaseOps.Domain.Common;
using BaseOps.Domain.Enums;

namespace BaseOps.Domain.Entities;

public sealed class Bulletin : AuditableEntity
{
    public Guid Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    public BulletinCategory Category { get; set; }
    
    public BulletinPriority Priority { get; set; }
    
    public BulletinScope Scope { get; set; }
    
    public Guid? SectionId { get; set; }
    
    public Guid? HangarId { get; set; }
    
    public Guid? ShopId { get; set; }
    
    public DateTime ExpiryDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool Pinned { get; set; } = false;
    
    public Guid CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public byte[] Version { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public Section? Section { get; set; }
    public Hangar? Hangar { get; set; }
    public Shop? Shop { get; set; }
    public ApplicationUser Creator { get; set; } = null!;
    public ICollection<BulletinAttachment> Attachments { get; set; } = new List<BulletinAttachment>();
    public ICollection<BulletinReadStatus> ReadStatuses { get; set; } = new List<BulletinReadStatus>();
    
    // Computed property for expiry
    public bool IsExpired => ExpiryDate < DateTime.UtcNow;
}
