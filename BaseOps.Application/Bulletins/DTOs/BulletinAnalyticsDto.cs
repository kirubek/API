namespace BaseOps.Application.Bulletins.DTOs;

public record BulletinAnalyticsDto
{
    public Guid BulletinId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int TotalRecipients { get; init; }
    public int ReadCount { get; init; }
    public int UnreadCount { get; init; }
    public double ReadRate { get; init; }
}
