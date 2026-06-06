using BaseOps.Application.DTOs;

namespace BaseOps.Infrastructure.Services;

public interface IAnalyticsService
{
    Task<ManpowerSummaryResponseDto> GetManpowerSummaryAsync(ManpowerSummaryRequestDto request, CancellationToken cancellationToken = default);
    Task<List<DailyManpowerSummaryDto>> GetDailyManpowerSummaryAsync(ManpowerSummaryRequestDto request, CancellationToken cancellationToken = default);
    Task<List<LeaveBalanceDto>> GetLeaveBalancesAsync(int year, Guid? sectionId, Guid? hangarId, CancellationToken cancellationToken = default);
    Task<TeamLeaderAnalyticsResponseDto> GetTeamLeaderAnalyticsAsync(TeamLeaderAnalyticsRequestDto request, CancellationToken cancellationToken = default);
}
