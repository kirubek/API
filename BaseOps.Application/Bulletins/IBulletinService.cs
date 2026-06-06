using BaseOps.Application.Bulletins.DTOs;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;

namespace BaseOps.Application.Bulletins;

public interface IBulletinService
{
    Task<BulletinDto> CreateBulletinAsync(CreateBulletinDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<BulletinDto?> GetBulletinAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<(List<BulletinDto> Items, int TotalCount)> GetBulletinsAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        BulletinCategory? category = null,
        BulletinPriority? priority = null,
        bool? unreadOnly = null,
        CancellationToken cancellationToken = default);
    Task<BulletinDto?> UpdateBulletinAsync(Guid id, UpdateBulletinDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteBulletinAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid bulletinId, Guid userId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<BulletinDto>> GetDashboardBulletinsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BulletinAnalyticsDto?> GetAnalyticsAsync(Guid bulletinId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<BulletinAnalyticsDto>> GetMyBulletinsAnalyticsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<BulletinDto>> GetArchiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BulletinAttachment> AddAttachmentAsync(Guid bulletinId, string generatedFileName, string originalFileName, string contentType, long fileSize, string storagePath, Guid userId, CancellationToken cancellationToken = default);
    Task<byte[]?> GetAttachmentContentAsync(Guid attachmentId, CancellationToken cancellationToken = default);
}
