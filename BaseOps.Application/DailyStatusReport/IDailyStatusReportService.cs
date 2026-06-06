using BaseOps.Application.DailyStatusReport.DTOs;

namespace BaseOps.Application.DailyStatusReport;

public interface IDailyStatusReportService
{
    Task<DailyStatusReportDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyStatusReportDto[]> ListAsync(int pageNumber = 1, int pageSize = 20, string? status = null, Guid? sectionId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<DailyStatusReportDto> CreateAsync(CreateDailyStatusReportDto dto, CancellationToken cancellationToken = default);
    Task<DailyStatusReportDto> UpdateAsync(Guid id, UpdateDailyStatusReportDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyStatusReportDto> SubmitAsync(Guid id, ReportDecisionDto dto, CancellationToken cancellationToken = default);
    Task<DailyStatusReportDto> ReviewAsync(Guid id, ReportDecisionDto dto, CancellationToken cancellationToken = default);
    Task<DailyStatusReportDto> ApproveAsync(Guid id, ReportDecisionDto dto, CancellationToken cancellationToken = default);
    Task<DailyStatusReportDto> RejectAsync(Guid id, ReportDecisionDto dto, CancellationToken cancellationToken = default);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<AnalyticsDto> GetAnalyticsAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<PhaseProgressDto[]> GetPhaseProgressAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<OverallStatusDto[]> GetOverallStatusAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<ImportValidationResultDto> ImportExcelAsync(ImportExcelDto dto, CancellationToken cancellationToken = default);
    Task RollbackImportAsync(Guid importHistoryId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportReportAsync(Guid reportId, string format, CancellationToken cancellationToken = default);
}
