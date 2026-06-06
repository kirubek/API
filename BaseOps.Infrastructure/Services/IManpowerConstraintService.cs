using BaseOps.Domain.Entities;

namespace BaseOps.Infrastructure.Services;

public interface IManpowerConstraintService
{
    Task<ManpowerConstraint> GetConstraintsAsync(Guid sectionId, Guid? hangarId, Guid? shopId, int year, CancellationToken cancellationToken = default);
    Task<bool> ValidateConstraintAsync(DateOnly date, int currentOnLeave, Guid sectionId, Guid? hangarId, Guid? shopId, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}
