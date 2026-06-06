using BaseOps.Domain.Enums;

namespace BaseOps.Application.Bulletins.DTOs;

public record BulletinDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public BulletinCategory Category { get; init; }
    public BulletinPriority Priority { get; init; }
    public BulletinScope Scope { get; init; }
    public Guid? SectionId { get; init; }
    public string? SectionName { get; init; }
    public Guid? HangarId { get; init; }
    public string? HangarName { get; init; }
    public Guid? ShopId { get; init; }
    public string? ShopName { get; init; }
    public DateTime ExpiryDate { get; init; }
    public bool IsActive { get; init; }
    public bool Pinned { get; init; }
    public bool IsExpired { get; init; }
    public Guid CreatedBy { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<BulletinAttachmentDto> Attachments { get; init; } = new();
    public bool IsRead { get; init; }
}

public record BulletinAttachmentDto
{
    public Guid Id { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string DownloadUrl { get; init; } = string.Empty;
}
