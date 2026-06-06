using BaseOps.Application.Common;
using BaseOps.Application.EmployeeProfiles;
using BaseOps.Application.EmployeeProfiles.DTOs;
using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaseOps.Infrastructure.EmployeeProfiles;

public sealed class EmployeeProfileService(
    BaseOpsDbContext dbContext,
    IAuditService auditService) : IEmployeeProfileService
{
    public async Task<EmployeeProfileResponseDto> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        return MapToDto(user);
    }

    public async Task<EmployeeProfileResponseDto> UpdateMyProfileAsync(Guid userId, UpdateEmployeeProfileDto dto, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FindAsync([userId], cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");

        var beforeValues = new
        {
            user.Email,
            user.PhoneNumber,
            user.Address,
            user.EmergencyContactName,
            user.EmergencyContactPhoneNumber,
            user.ProfilePhotoUrl
        };

        // Apply updates (only editable fields)
        if (dto.Email != null) user.Email = dto.Email;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.Address != null) user.Address = dto.Address;
        if (dto.EmergencyContactName != null) user.EmergencyContactName = dto.EmergencyContactName;
        if (dto.EmergencyContactPhoneNumber != null) user.EmergencyContactPhoneNumber = dto.EmergencyContactPhoneNumber;
        if (dto.ProfilePhotoUrl != null) user.ProfilePhotoUrl = dto.ProfilePhotoUrl;

        user.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var afterValues = new
        {
            user.Email,
            user.PhoneNumber,
            user.Address,
            user.EmergencyContactName,
            user.EmergencyContactPhoneNumber,
            user.ProfilePhotoUrl
        };

        await auditService.WriteAsync(
            userId,
            "PROFILE_UPDATED",
            "ApplicationUser",
            user.Id.ToString(),
            beforeValues,
            afterValues,
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        // Reload with includes for response
        var updatedUser = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        return MapToDto(updatedUser!);
    }

    public async Task<EmployeeProfileResponseDto> GetProfileByIdAsync(Guid currentUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user not found");

        var targetUser = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == targetUserId, cancellationToken);

        if (targetUser == null)
            throw new KeyNotFoundException("Target user not found");

        // IDOR protection: Only allow access if:
        // 1. Viewing own profile
        // 2. User is SystemAdmin or Director
        // 3. User is Manager and target is in same section
        // 4. User is TeamLeader and target is in same hangar/shop
        if (currentUserId != targetUserId)
        {
            var hasAccess = currentUser.Role switch
            {
                UserRole.SystemAdmin or UserRole.Director => true,
                UserRole.Manager when currentUser.SectionId.HasValue && targetUser.SectionId == currentUser.SectionId => true,
                UserRole.TeamLeader when currentUser.HangarId.HasValue && targetUser.HangarId == currentUser.HangarId => true,
                UserRole.TeamLeader when currentUser.ShopId.HasValue && targetUser.ShopId == currentUser.ShopId => true,
                _ => false
            };

            if (!hasAccess)
            {
                await auditService.WriteAsync(
                    currentUserId,
                    "PROFILE_ACCESS_DENIED",
                    "ApplicationUser",
                    targetUserId.ToString(),
                    null,
                    null,
                    true,
                    null,
                    Guid.NewGuid().ToString(),
                    cancellationToken);

                throw new UnauthorizedAccessException("You do not have permission to view this profile");
            }
        }

        return MapToDto(targetUser);
    }

    public async Task<PaginatedResult<EmployeeProfileResponseDto>> GetEmployeeProfilesAsync(
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        Guid? sectionId = null,
        Guid? hangarId = null,
        Guid? shopId = null,
        Guid? teamLeaderId = null,
        string? employeeId = null,
        string? firstName = null,
        string? lastName = null,
        string? position = null,
        CancellationToken cancellationToken = default)
    {
        // Pagination validation
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var currentUser = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user not found");

        var query = dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .Where(x => x.IsActive);

        // Apply RBAC scope filtering based on user role
        query = currentUser.Role switch
        {
            UserRole.Employee => query.Where(x => x.Id == currentUserId),
            UserRole.TeamLeader when currentUser.HangarId.HasValue => query.Where(x => x.HangarId == currentUser.HangarId),
            UserRole.TeamLeader when currentUser.ShopId.HasValue => query.Where(x => x.ShopId == currentUser.ShopId),
            UserRole.TeamLeader => query.Where(x => false), // Unassigned TeamLeader sees nothing
            UserRole.Manager when currentUser.SectionId.HasValue => query.Where(x => x.SectionId == currentUser.SectionId),
            UserRole.Manager => query.Where(x => false), // Unassigned Manager sees nothing
            UserRole.SystemAdmin or UserRole.Director or UserRole.SafetyInspector => query,
            _ => query.Where(x => x.Id == currentUserId)
        };

        // Apply filter parameters (only if user has elevated permissions)
        if (currentUser.Role is UserRole.SystemAdmin or UserRole.Director or UserRole.Manager or UserRole.SafetyInspector)
        {
            if (sectionId.HasValue) query = query.Where(x => x.SectionId == sectionId.Value);
            if (hangarId.HasValue) query = query.Where(x => x.HangarId == hangarId.Value);
            if (shopId.HasValue) query = query.Where(x => x.ShopId == shopId.Value);
            if (teamLeaderId.HasValue) query = query.Where(x => x.ReportsToUserId == teamLeaderId.Value);
        }

        // Apply search filters (available to all roles within their scope)
        if (!string.IsNullOrEmpty(employeeId))
            query = query.Where(x => x.EmployeeId.Contains(employeeId));

        if (!string.IsNullOrEmpty(firstName))
            query = query.Where(x => x.FullName.Contains(firstName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(lastName))
            query = query.Where(x => x.FullName.Contains(lastName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(position))
            query = query.Where(x => x.Role.ToString().Contains(position, StringComparison.OrdinalIgnoreCase));

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.EmployeeId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);

        var totalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling((double)total / pageSize);
        return new PaginatedResult<EmployeeProfileResponseDto>(items, total, pageNumber, pageSize, totalPages);
    }

    public async Task<EmployeeProfileResponseDto> AdminUpdateProfileAsync(Guid currentUserId, Guid targetUserId, AdminUpdateEmployeeProfileDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user not found");

        // Only SystemAdmin and Director can perform admin updates
        if (currentUser.Role is not UserRole.SystemAdmin and not UserRole.Director)
            throw new UnauthorizedAccessException("You do not have permission to perform this action");

        var targetUser = await dbContext.Users
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == targetUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Target user not found");

        var beforeValues = new
        {
            targetUser.FullName,
            targetUser.Email,
            targetUser.Role,
            targetUser.SectionId,
            targetUser.HangarId,
            targetUser.ShopId,
            targetUser.ReportsToUserId,
            targetUser.IsActive,
            targetUser.MustChangePassword,
            targetUser.PhoneNumber,
            targetUser.Address,
            targetUser.EmergencyContactName,
            targetUser.EmergencyContactPhoneNumber,
            targetUser.ProfilePhotoUrl
        };

        // Apply updates
        if (dto.FullName != null) targetUser.FullName = dto.FullName;
        if (dto.Email != null) targetUser.Email = dto.Email;
        if (dto.Role.HasValue) targetUser.Role = dto.Role.Value;
        if (dto.SectionId.HasValue) targetUser.SectionId = dto.SectionId.Value;
        if (dto.HangarId.HasValue) targetUser.HangarId = dto.HangarId.Value;
        if (dto.ShopId.HasValue) targetUser.ShopId = dto.ShopId.Value;
        if (dto.ReportsToUserId.HasValue) targetUser.ReportsToUserId = dto.ReportsToUserId.Value;
        if (dto.IsActive.HasValue) targetUser.IsActive = dto.IsActive.Value;
        if (dto.MustChangePassword.HasValue) targetUser.MustChangePassword = dto.MustChangePassword.Value;
        if (dto.PhoneNumber != null) targetUser.PhoneNumber = dto.PhoneNumber;
        if (dto.Address != null) targetUser.Address = dto.Address;
        if (dto.EmergencyContactName != null) targetUser.EmergencyContactName = dto.EmergencyContactName;
        if (dto.EmergencyContactPhoneNumber != null) targetUser.EmergencyContactPhoneNumber = dto.EmergencyContactPhoneNumber;
        if (dto.ProfilePhotoUrl != null) targetUser.ProfilePhotoUrl = dto.ProfilePhotoUrl;

        targetUser.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var afterValues = new
        {
            targetUser.FullName,
            targetUser.Email,
            targetUser.Role,
            targetUser.SectionId,
            targetUser.HangarId,
            targetUser.ShopId,
            targetUser.ReportsToUserId,
            targetUser.IsActive,
            targetUser.MustChangePassword,
            targetUser.PhoneNumber,
            targetUser.Address,
            targetUser.EmergencyContactName,
            targetUser.EmergencyContactPhoneNumber,
            targetUser.ProfilePhotoUrl
        };

        await auditService.WriteAsync(
            currentUserId,
            "PROFILE_ADMIN_UPDATED",
            "ApplicationUser",
            targetUser.Id.ToString(),
            beforeValues,
            afterValues,
            false,
            null,
            Guid.NewGuid().ToString(),
            cancellationToken);

        // Reload with includes for response
        var updatedUser = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == targetUserId, cancellationToken);

        return MapToDto(updatedUser!);
    }

    private static EmployeeProfileResponseDto MapToDto(ApplicationUser user)
    {
        var nameParts = user.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return new EmployeeProfileResponseDto
        {
            Id = user.Id,
            EmployeeId = user.EmployeeId,
            FirstName = nameParts.FirstOrDefault() ?? user.FullName,
            LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Address = user.Address ?? string.Empty,
            Position = user.Position ?? user.Role.ToString(),
            EmergencyContactName = user.EmergencyContactName ?? string.Empty,
            EmergencyContactPhoneNumber = user.EmergencyContactPhoneNumber ?? string.Empty,
            MaintenanceAuthorizationType = user.CompanyAuthorizationType?.ToString() ?? string.Empty,
            Role = user.Role == UserRole.SystemAdmin ? "SystemAdministrator" : user.Role == UserRole.SafetyInspector ? "SafaInspector" : user.Role.ToString(),
            RoleName = user.Role.ToString(),
            SectionId = user.SectionId,
            SectionName = user.Section?.Name,
            HangarId = user.HangarId,
            HangarName = user.Hangar?.Name,
            ShopId = user.ShopId,
            ShopName = user.Shop?.Name,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            LastLoginAt = user.LastLoginAt,
            HireDate = user.CreatedAt,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
