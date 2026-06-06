namespace BaseOps.Domain.Entities;

public sealed class BulletinReadStatus
{
    public Guid Id { get; set; }
    
    public Guid BulletinId { get; set; }
    
    public Guid UserId { get; set; }
    
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Bulletin Bulletin { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
