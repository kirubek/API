using BaseOps.Domain.Enums;

namespace BaseOps.Application.Bulletins.DTOs;

public record CreateBulletinDto
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public BulletinCategory Category { get; init; }
    public BulletinPriority Priority { get; init; }
    public DateTime ExpiryDate { get; init; }
    public bool Pinned { get; init; } = false;
    public List<CreateBulletinAttachmentDto>? Attachments { get; init; }
}

public record CreateBulletinAttachmentDto
{
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string FileContent { get; init; } = string.Empty; // Base64 encoded string
}
