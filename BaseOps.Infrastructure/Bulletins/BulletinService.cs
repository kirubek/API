using BaseOps.Application.Bulletins;
using BaseOps.Application.Bulletins.DTOs;
using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace BaseOps.Infrastructure.Bulletins;

public sealed class BulletinService(BaseOpsDbContext dbContext, IAuditService auditService, IAttachmentSecurityService attachmentService) : IBulletinService
{
    public async Task<BulletinDto> CreateBulletinAsync(CreateBulletinDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FindAsync([userId], cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        // Determine scope based on user role
        var (scope, sectionId, hangarId, shopId) = ResolveUserScope(user);

        // Validate scope assignment
        ValidateScopeAssignment(scope, user.Role);

        var bulletin = new Bulletin
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Content = dto.Content,
            Category = dto.Category,
            Priority = dto.Priority,
            Scope = scope,
            SectionId = sectionId,
            HangarId = hangarId,
            ShopId = shopId,
            ExpiryDate = dto.ExpiryDate,
            Pinned = dto.Pinned,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        // Add attachments if provided
        if (dto.Attachments != null && dto.Attachments.Count > 0)
        {
            foreach (var attachmentDto in dto.Attachments)
            {
                // Convert base64 string to byte array
                var fileBytes = Convert.FromBase64String(attachmentDto.FileContent);
                
                // Store file using attachment service
                var (generatedFileName, storagePath) = await attachmentService.StoreAttachmentAsync(
                    attachmentDto.OriginalFileName,
                    attachmentDto.ContentType,
                    fileBytes,
                    userId,
                    cancellationToken);
                
                var attachment = new BulletinAttachment
                {
                    Id = Guid.NewGuid(),
                    BulletinId = bulletin.Id,
                    OriginalFileName = attachmentDto.OriginalFileName,
                    GeneratedFileName = generatedFileName,
                    ContentType = attachmentDto.ContentType,
                    FileSize = fileBytes.Length,
                    StoragePath = storagePath,
                    UploadedBy = userId,
                    UploadTimestamp = DateTime.UtcNow
                };
                bulletin.Attachments.Add(attachment);
            }
        }

        dbContext.Bulletins.Add(bulletin);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(userId, "BulletinCreated", "Bulletin", bulletin.Id.ToString(), null, null, false, null, Guid.NewGuid().ToString(), cancellationToken);

        return await MapToDtoAsync(bulletin, userId, cancellationToken);
    }

    public async Task<BulletinDto?> GetBulletinAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FindAsync([userId], cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        var bulletin = await dbContext.Bulletins
            .Include(b => b.Attachments)
            .Include(b => b.Creator)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (bulletin == null) return null;

        // Check if user has access to this bulletin
        if (!HasAccessToBulletin(bulletin, user))
            throw new UnauthorizedAccessException("Access denied to this bulletin");

        return await MapToDtoAsync(bulletin, userId, cancellationToken);
    }

    public async Task<(List<BulletinDto> Items, int TotalCount)> GetBulletinsAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        BulletinCategory? category = null,
        BulletinPriority? priority = null,
        bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FindAsync([userId], cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        var query = dbContext.Bulletins
            .Include(b => b.Attachments)
            .Include(b => b.Creator)
            .Where(b => b.IsActive);

        // Apply scope-based filtering
        query = ApplyScopeFilter(query, user);

        // Apply category filter
        if (category.HasValue)
            query = query.Where(b => b.Category == category.Value);

        // Apply priority filter
        if (priority.HasValue)
            query = query.Where(b => b.Priority == priority.Value);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var bulletins = await query
            .OrderByDescending(b => b.Pinned)
            .ThenByDescending(b => b.Priority)
            .ThenByDescending(b => b.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var dtos = new List<BulletinDto>();
        foreach (var bulletin in bulletins)
        {
            var dto = await MapToDtoAsync(bulletin, userId, cancellationToken);
            
            // Apply unread filter
            if (unreadOnly == true && dto.IsRead)
                continue;
            
            dtos.Add(dto);
        }

        return (dtos, totalCount);
    }

    public async Task<BulletinDto?> UpdateBulletinAsync(Guid id, UpdateBulletinDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var bulletin = await dbContext.Bulletins
            .Include(b => b.Attachments)
            .Include(b => b.Creator)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException("Bulletin not found");

        var user = await dbContext.Users.FindAsync([userId], cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        // Check ownership or Director role
        if (bulletin.CreatedBy != userId && user.Role != UserRole.Director)
            throw new UnauthorizedAccessException("You can only edit your own bulletins");

        // Update fields if provided
        if (dto.Title != null) bulletin.Title = dto.Title;
        if (dto.Content != null) bulletin.Content = dto.Content;
        if (dto.Category.HasValue) bulletin.Category = dto.Category.Value;
        if (dto.Priority.HasValue) bulletin.Priority = dto.Priority.Value;
        if (dto.ExpiryDate.HasValue) bulletin.ExpiryDate = dto.ExpiryDate.Value;
        if (dto.Pinned.HasValue) bulletin.Pinned = dto.Pinned.Value;
        if (dto.IsActive.HasValue) bulletin.IsActive = dto.IsActive.Value;

        bulletin.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(userId, "BulletinUpdated", "Bulletin", bulletin.Id.ToString(), null, null, false, null, Guid.NewGuid().ToString(), cancellationToken);

        return await MapToDtoAsync(bulletin, userId, cancellationToken);
    }

    public async Task DeleteBulletinAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var bulletin = await dbContext.Bulletins.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Bulletin not found");

        var user = await dbContext.Users.FindAsync([userId], cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        // Check ownership or Director role
        if (bulletin.CreatedBy != userId && user.Role != UserRole.Director)
            throw new UnauthorizedAccessException("You can only delete your own bulletins");

        // Soft delete
        bulletin.IsActive = false;
        bulletin.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(userId, "BulletinDeleted", "Bulletin", bulletin.Id.ToString(), null, null, false, null, Guid.NewGuid().ToString(), cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid bulletinId, Guid userId, CancellationToken cancellationToken = default)
    {
        var bulletin = await dbContext.Bulletins.FindAsync([bulletinId], cancellationToken)
            ?? throw new NotFoundException("Bulletin not found");

        var user = await dbContext.Users.FindAsync([userId], cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        // Check access
        if (!HasAccessToBulletin(bulletin, user))
            throw new UnauthorizedAccessException("Access denied to this bulletin");

        // Upsert read status
        var existingStatus = await dbContext.BulletinReadStatuses
            .FirstOrDefaultAsync(rs => rs.BulletinId == bulletinId && rs.UserId == userId, cancellationToken);

        if (existingStatus == null)
        {
            dbContext.BulletinReadStatuses.Add(new BulletinReadStatus
            {
                Id = Guid.NewGuid(),
                BulletinId = bulletinId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            });
        }
        else
        {
            existingStatus.ReadAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(userId, "BulletinRead", "Bulletin", bulletinId.ToString(), null, null, false, null, Guid.NewGuid().ToString(), cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(u => u.Section)
            .Include(u => u.Hangar)
            .Include(u => u.Shop)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        // Get all accessible bulletins
        var accessibleBulletinIds = await dbContext.Bulletins
            .Where(b => b.IsActive)
            .Where(b => HasAccessToBulletin(b, user))
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        // Get already read bulletin IDs
        var alreadyReadIds = await dbContext.BulletinReadStatuses
            .Where(rs => rs.UserId == userId)
            .Select(rs => rs.BulletinId)
            .ToListAsync(cancellationToken);

        // Mark unread bulletins as read
        var unreadIds = accessibleBulletinIds.Except(alreadyReadIds).ToList();
        foreach (var bulletinId in unreadIds)
        {
            dbContext.BulletinReadStatuses.Add(new BulletinReadStatus
            {
                Id = Guid.NewGuid(),
                BulletinId = bulletinId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(userId, "BulletinReadAll", "Bulletin", null, null, null, false, null, Guid.NewGuid().ToString(), cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(u => u.Section)
            .Include(u => u.Hangar)
            .Include(u => u.Shop)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        var accessibleBulletinIds = await dbContext.Bulletins
            .Where(b => b.IsActive)
            .Where(b => HasAccessToBulletin(b, user))
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var readBulletinIds = await dbContext.BulletinReadStatuses
            .Where(rs => rs.UserId == userId)
            .Select(rs => rs.BulletinId)
            .ToListAsync(cancellationToken);

        return accessibleBulletinIds.Except(readBulletinIds).Count();
    }

    public async Task<List<BulletinDto>> GetDashboardBulletinsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(u => u.Section)
            .Include(u => u.Hangar)
            .Include(u => u.Shop)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        var query = dbContext.Bulletins
            .Include(b => b.Attachments)
            .Include(b => b.Creator)
            .Where(b => b.IsActive);

        // Apply scope filter at database level
        query = ApplyScopeFilter(query, user);

        var bulletins = await query
            .OrderByDescending(b => b.Pinned)
            .ThenByDescending(b => b.Priority)
            .ThenByDescending(b => b.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var dtos = new List<BulletinDto>();
        foreach (var bulletin in bulletins)
        {
            dtos.Add(await MapToDtoAsync(bulletin, userId, cancellationToken));
        }

        return dtos;
    }

    public async Task<BulletinAnalyticsDto?> GetAnalyticsAsync(Guid bulletinId, Guid userId, CancellationToken cancellationToken = default)
    {
        var bulletin = await dbContext.Bulletins
            .Include(b => b.Creator)
            .FirstOrDefaultAsync(b => b.Id == bulletinId, cancellationToken)
            ?? throw new NotFoundException("Bulletin not found");

        var user = await dbContext.Users.FindAsync([userId], cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        // Only Directors or bulletin creators can view analytics
        if (user.Role != UserRole.Director && bulletin.CreatedBy != userId)
            throw new UnauthorizedAccessException("Access denied to analytics");

        // Calculate total recipients based on scope
        var totalRecipients = await CalculateTotalRecipientsAsync(bulletin, cancellationToken);

        // Get read count
        var readCount = await dbContext.BulletinReadStatuses
            .CountAsync(rs => rs.BulletinId == bulletinId, cancellationToken);

        var unreadCount = totalRecipients - readCount;
        var readRate = totalRecipients > 0 ? (double)readCount / totalRecipients * 100 : 0;

        return new BulletinAnalyticsDto
        {
            BulletinId = bulletinId,
            Title = bulletin.Title,
            Category = bulletin.Category.ToString(),
            Priority = bulletin.Priority.ToString(),
            CreatedAt = bulletin.CreatedAt,
            TotalRecipients = totalRecipients,
            ReadCount = readCount,
            UnreadCount = unreadCount,
            ReadRate = Math.Round(readRate, 2)
        };
    }

    public async Task<List<BulletinAnalyticsDto>> GetMyBulletinsAnalyticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FindAsync([userId], cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        // Only TeamLeaders, Managers, and Directors can view their bulletin analytics
        if (user.Role != UserRole.TeamLeader && user.Role != UserRole.Manager && user.Role != UserRole.Director)
            throw new UnauthorizedAccessException("Access denied to analytics");

        // Get all bulletins created by this user
        var bulletins = await dbContext.Bulletins
            .Where(b => b.CreatedBy == userId && b.IsActive)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var analyticsList = new List<BulletinAnalyticsDto>();
        foreach (var bulletin in bulletins)
        {
            var totalRecipients = await CalculateTotalRecipientsAsync(bulletin, cancellationToken);
            var readCount = await dbContext.BulletinReadStatuses
                .CountAsync(rs => rs.BulletinId == bulletin.Id, cancellationToken);
            var unreadCount = totalRecipients - readCount;
            var readRate = totalRecipients > 0 ? (double)readCount / totalRecipients * 100 : 0;

            analyticsList.Add(new BulletinAnalyticsDto
            {
                BulletinId = bulletin.Id,
                Title = bulletin.Title,
                Category = bulletin.Category.ToString(),
                Priority = bulletin.Priority.ToString(),
                CreatedAt = bulletin.CreatedAt,
                TotalRecipients = totalRecipients,
                ReadCount = readCount,
                UnreadCount = unreadCount,
                ReadRate = Math.Round(readRate, 2)
            });
        }

        return analyticsList;
    }

    public async Task<List<BulletinDto>> GetArchiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(u => u.Section)
            .Include(u => u.Hangar)
            .Include(u => u.Shop)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        // Only Directors can access archive
        if (user.Role != UserRole.Director)
            throw new UnauthorizedAccessException("Access denied to archive");

        var bulletins = await dbContext.Bulletins
            .Include(b => b.Attachments)
            .Include(b => b.Creator)
            .Where(b => !b.IsActive || b.IsExpired)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = new List<BulletinDto>();
        foreach (var bulletin in bulletins)
        {
            dtos.Add(await MapToDtoAsync(bulletin, userId, cancellationToken));
        }

        return dtos;
    }

    private (BulletinScope Scope, Guid? SectionId, Guid? HangarId, Guid? ShopId) ResolveUserScope(ApplicationUser user)
    {
        return user.Role switch
        {
            UserRole.Director => (BulletinScope.Global, null, null, null),
            UserRole.Manager => (BulletinScope.Section, user.SectionId, null, null),
            UserRole.TeamLeader => (BulletinScope.Hangar, user.SectionId, user.HangarId, user.ShopId),
            _ => throw new UnauthorizedAccessException("You do not have permission to create bulletins")
        };
    }

    private void ValidateScopeAssignment(BulletinScope scope, UserRole role)
    {
        switch (role)
        {
            case UserRole.Director:
                if (scope != BulletinScope.Global)
                    throw new UnauthorizedAccessException("Directors can only create global bulletins");
                break;
            case UserRole.Manager:
                if (scope != BulletinScope.Section)
                    throw new UnauthorizedAccessException("Managers can only create section bulletins");
                break;
            case UserRole.TeamLeader:
                if (scope != BulletinScope.Hangar)
                    throw new UnauthorizedAccessException("Team Leaders can only create hangar/shop bulletins");
                break;
            default:
                throw new UnauthorizedAccessException("You do not have permission to create bulletins");
        }
    }

    private bool HasAccessToBulletin(Bulletin bulletin, ApplicationUser user)
    {
        // Directors can access all bulletins
        if (user.Role == UserRole.Director) return true;

        // Managers can access section and global bulletins
        if (user.Role == UserRole.Manager)
        {
            return bulletin.Scope == BulletinScope.Global ||
                   (bulletin.Scope == BulletinScope.Section && bulletin.SectionId == user.SectionId);
        }

        // Team Leaders can access hangar, section, and global bulletins
        if (user.Role == UserRole.TeamLeader)
        {
            return bulletin.Scope == BulletinScope.Global ||
                   (bulletin.Scope == BulletinScope.Section && bulletin.SectionId == user.SectionId) ||
                   (bulletin.Scope == BulletinScope.Hangar && 
                    (bulletin.HangarId == user.HangarId || bulletin.ShopId == user.ShopId));
        }

        // Employees can access hangar, section, and global bulletins
        if (user.Role == UserRole.Employee)
        {
            return bulletin.Scope == BulletinScope.Global ||
                   (bulletin.Scope == BulletinScope.Section && bulletin.SectionId == user.SectionId) ||
                   (bulletin.Scope == BulletinScope.Hangar && 
                    (bulletin.HangarId == user.HangarId || bulletin.ShopId == user.ShopId));
        }

        return false;
    }

    private IQueryable<Bulletin> ApplyScopeFilter(IQueryable<Bulletin> query, ApplicationUser user)
    {
        // Directors see all bulletins
        if (user.Role == UserRole.Director) return query;

        // Managers see section and global bulletins
        if (user.Role == UserRole.Manager)
        {
            return query.Where(b => b.Scope == BulletinScope.Global ||
                                   (b.Scope == BulletinScope.Section && b.SectionId == user.SectionId));
        }

        // Team Leaders and Employees see hangar, section, and global bulletins
        return query.Where(b => b.Scope == BulletinScope.Global ||
                               (b.Scope == BulletinScope.Section && b.SectionId == user.SectionId) ||
                               (b.Scope == BulletinScope.Hangar && 
                                (b.HangarId == user.HangarId || b.ShopId == user.ShopId)));
    }

    private async Task<int> CalculateTotalRecipientsAsync(Bulletin bulletin, CancellationToken cancellationToken)
    {
        return bulletin.Scope switch
        {
            BulletinScope.Global => await dbContext.Users.CountAsync(u => true, cancellationToken),
            BulletinScope.Section => await dbContext.Users.CountAsync(u => u.SectionId == bulletin.SectionId, cancellationToken),
            BulletinScope.Hangar => await dbContext.Users.CountAsync(u => 
                (bulletin.HangarId.HasValue && u.HangarId == bulletin.HangarId) ||
                (bulletin.ShopId.HasValue && u.ShopId == bulletin.ShopId), cancellationToken),
            _ => 0
        };
    }

    private async Task<BulletinDto> MapToDtoAsync(Bulletin bulletin, Guid userId, CancellationToken cancellationToken)
    {
        var isRead = await dbContext.BulletinReadStatuses
            .AnyAsync(rs => rs.BulletinId == bulletin.Id && rs.UserId == userId, cancellationToken);

        // Fetch names for section, hangar, and shop
        string? sectionName = null;
        string? hangarName = null;
        string? shopName = null;

        if (bulletin.SectionId.HasValue)
        {
            var section = await dbContext.Sections
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == bulletin.SectionId.Value, cancellationToken);
            sectionName = section?.Name;
        }

        if (bulletin.HangarId.HasValue)
        {
            var hangar = await dbContext.Hangars
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == bulletin.HangarId.Value, cancellationToken);
            hangarName = hangar?.Name;
        }

        if (bulletin.ShopId.HasValue)
        {
            var shop = await dbContext.Shops
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == bulletin.ShopId.Value, cancellationToken);
            shopName = shop?.Name;
        }

        return new BulletinDto
        {
            Id = bulletin.Id,
            Title = bulletin.Title,
            Content = bulletin.Content,
            Category = bulletin.Category,
            Priority = bulletin.Priority,
            Scope = bulletin.Scope,
            SectionId = bulletin.SectionId,
            SectionName = sectionName,
            HangarId = bulletin.HangarId,
            HangarName = hangarName,
            ShopId = bulletin.ShopId,
            ShopName = shopName,
            ExpiryDate = bulletin.ExpiryDate,
            IsActive = bulletin.IsActive,
            Pinned = bulletin.Pinned,
            IsExpired = bulletin.IsExpired,
            CreatedBy = bulletin.CreatedBy,
            CreatedByName = bulletin.Creator?.FullName ?? "Unknown",
            CreatedAt = bulletin.CreatedAt,
            UpdatedAt = bulletin.UpdatedAt,
            Attachments = bulletin.Attachments.Select(a => new BulletinAttachmentDto
            {
                Id = a.Id,
                OriginalFileName = a.OriginalFileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                DownloadUrl = $"/api/bulletin/{bulletin.Id}/attachment/{a.Id}"
            }).ToList(),
            IsRead = isRead
        };
    }

    public async Task<BulletinAttachment> AddAttachmentAsync(Guid bulletinId, string generatedFileName, string originalFileName, string contentType, long fileSize, string storagePath, Guid userId, CancellationToken cancellationToken = default)
    {
        var bulletin = await dbContext.Bulletins.FindAsync([bulletinId], cancellationToken)
            ?? throw new NotFoundException("Bulletin not found");

        var attachment = new BulletinAttachment
        {
            Id = Guid.NewGuid(),
            BulletinId = bulletinId,
            OriginalFileName = originalFileName,
            GeneratedFileName = generatedFileName,
            ContentType = contentType,
            FileSize = fileSize,
            StoragePath = storagePath,
            UploadedBy = userId,
            UploadTimestamp = DateTime.UtcNow
        };

        dbContext.BulletinAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(userId, "BulletinAttachmentAdded", "BulletinAttachment", attachment.Id.ToString(), null, null, false, null, Guid.NewGuid().ToString(), cancellationToken);

        return attachment;
    }

    public async Task<byte[]?> GetAttachmentContentAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.BulletinAttachments.FindAsync([attachmentId], cancellationToken);
        if (attachment == null) return null;

        // Retrieve file from storage service
        var fileContent = await attachmentService.GetAttachmentContentAsync(attachment.StoragePath, cancellationToken);
        return fileContent;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
