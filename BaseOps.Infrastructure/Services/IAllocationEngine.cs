using BaseOps.Application.DTOs;
using BaseOps.Domain.Entities;

namespace BaseOps.Infrastructure.Services;

public interface IAllocationEngine
{
    Task<List<AnnualLeavePlanEntry>> GeneratePlanEntriesAsync(
        List<AnnualLeaveRequest> requests,
        GeneratePlanDto planDto,
        CancellationToken cancellationToken = default);
}
