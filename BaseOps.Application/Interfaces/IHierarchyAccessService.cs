using BaseOps.Domain.Entities;
using BaseOps.Domain.Interfaces;

namespace BaseOps.Application.Interfaces;

public interface IHierarchyAccessService
{
    IQueryable<T> ApplyScopeFilter<T>(IQueryable<T> query, ApplicationUser currentUser) where T : class, IScopedEntity;
}
