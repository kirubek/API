using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Domain.Interfaces;

namespace BaseOps.Infrastructure.Authorization;

public sealed class HierarchyAccessService : IHierarchyAccessService
{
    public IQueryable<T> ApplyScopeFilter<T>(IQueryable<T> query, ApplicationUser currentUser) where T : class, IScopedEntity
    {
        return currentUser.Role switch
        {
            UserRole.SystemAdmin or UserRole.Director => query,
            UserRole.Manager when currentUser.SectionId.HasValue => query.Where(x => x.SectionId == currentUser.SectionId.Value),
            UserRole.TeamLeader when currentUser.SectionId.HasValue && currentUser.HangarId.HasValue => query.Where(x => x.SectionId == currentUser.SectionId.Value && x.HangarId == currentUser.HangarId.Value),
            UserRole.TeamLeader when currentUser.SectionId.HasValue && currentUser.ShopId.HasValue => query.Where(x => x.SectionId == currentUser.SectionId.Value && x.ShopId == currentUser.ShopId.Value),
            UserRole.Employee or UserRole.SafetyInspector => query.Where(x => x.AssignedUserId == currentUser.Id),
            _ => query.Where(_ => false)
        };
    }
}
