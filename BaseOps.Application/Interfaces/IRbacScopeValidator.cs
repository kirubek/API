using BaseOps.Domain.Entities;

namespace BaseOps.Application.Interfaces;

public interface IRbacScopeValidator
{
    bool CanAccess<T>(ApplicationUser currentUser, T entity);
}
