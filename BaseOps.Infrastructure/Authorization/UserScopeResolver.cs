using BaseOps.Application.Common;
using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;

namespace BaseOps.Infrastructure.Authorization;

public sealed class UserScopeResolver : IUserScopeResolver
{
    public UserScope Resolve(ApplicationUser user)
    {
        var hasProductionPlannerAccess = string.Equals(user.Section?.Name, "Technical Support-Base", StringComparison.OrdinalIgnoreCase);
        var hasOperationalScope = user.Role switch
        {
            UserRole.SystemAdmin or UserRole.Director => true,
            UserRole.Manager => user.SectionId.HasValue,
            UserRole.TeamLeader => user.SectionId.HasValue && (user.HangarId.HasValue || user.ShopId.HasValue),
            _ => true
        };

        // Context-based permissions for Technical Support-Base section
        var isTechnicalSupportBase = string.Equals(user.Section?.Name, "Technical Support-Base", StringComparison.OrdinalIgnoreCase);
        var canCreateAumsReports = isTechnicalSupportBase && (user.Role == UserRole.TeamLeader || user.Role == UserRole.Employee);
        var canCreateCarryOverReports = isTechnicalSupportBase && (user.Role == UserRole.TeamLeader || user.Role == UserRole.Employee);
        // Only TeamLeaders and Employees in Technical Support-Base can create post-mortem reports, not Directors or Managers
        var canCreatePostMortemReports = isTechnicalSupportBase && (user.Role == UserRole.TeamLeader || user.Role == UserRole.Employee);

        return new UserScope(user.Id, user.Role.ToString(), user.SectionId, user.HangarId, user.ShopId, hasOperationalScope, hasProductionPlannerAccess, canCreateAumsReports, canCreateCarryOverReports, canCreatePostMortemReports);
    }
}
