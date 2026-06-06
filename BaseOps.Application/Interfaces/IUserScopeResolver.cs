using BaseOps.Application.Common;
using BaseOps.Domain.Entities;

namespace BaseOps.Application.Interfaces;

public interface IUserScopeResolver
{
    UserScope Resolve(ApplicationUser user);
}
