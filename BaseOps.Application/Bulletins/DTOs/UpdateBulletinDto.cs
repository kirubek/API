using BaseOps.Domain.Enums;

namespace BaseOps.Application.Bulletins.DTOs;

public record UpdateBulletinDto
{
    public string? Title { get; init; }
    public string? Content { get; init; }
    public BulletinCategory? Category { get; init; }
    public BulletinPriority? Priority { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public bool? Pinned { get; init; }
    public bool? IsActive { get; init; }
}
