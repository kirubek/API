using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Domain.Interfaces;

namespace BaseOps.Infrastructure.Authorization;

public sealed class RbacScopeValidator : IRbacScopeValidator
{
    public bool CanAccess<T>(ApplicationUser currentUser, T entity)
    {
        if (entity is not IScopedEntity scoped)
        {
            return currentUser.Role is UserRole.SystemAdmin or UserRole.Director;
        }

        return currentUser.Role switch
        {
            UserRole.SystemAdmin or UserRole.Director => true,
            UserRole.Manager => currentUser.SectionId.HasValue && scoped.SectionId == currentUser.SectionId,
            UserRole.TeamLeader when currentUser.SectionId.HasValue && currentUser.HangarId.HasValue => scoped.SectionId == currentUser.SectionId && scoped.HangarId == currentUser.HangarId,
            UserRole.TeamLeader when currentUser.SectionId.HasValue && currentUser.ShopId.HasValue => scoped.SectionId == currentUser.SectionId && scoped.ShopId == currentUser.ShopId,
            UserRole.TeamLeader => false,
            UserRole.Employee or UserRole.SafetyInspector => scoped.AssignedUserId == currentUser.Id,
            _ => false
        };
    }
}
