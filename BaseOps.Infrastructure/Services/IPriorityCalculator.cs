using BaseOps.Domain.Entities;

namespace BaseOps.Infrastructure.Services;

public interface IPriorityCalculator
{
    int CalculatePriorityScore(AnnualLeaveRequest request, SourceChoice sourceChoice);
    void SortRequestsByPriority(List<AnnualLeaveRequest> requests);
}
