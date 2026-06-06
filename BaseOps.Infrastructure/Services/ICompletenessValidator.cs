using BaseOps.Application.DTOs;
using BaseOps.Domain.Entities;

namespace BaseOps.Infrastructure.Services;

public interface ICompletenessValidator
{
    Task<AnnualLeaveStatusResponseDto> ValidateCompletenessAsync(AnnualLeaveStatusRequestDto request, CancellationToken cancellationToken = default);
}
