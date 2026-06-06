using BaseOps.Application.DailyStatusReport;
using BaseOps.Application.DailyStatusReport.DTOs;
using BaseOps.Domain.Entities;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DomainDailyStatusReport = BaseOps.Domain.Entities.DailyStatusReport;
using DomainTaskStatus = BaseOps.Domain.Entities.TaskStatus;

namespace BaseOps.Infrastructure.DailyStatusReport;

public sealed class DailyStatusReportService(BaseOpsDbContext dbContext) : IDailyStatusReportService
{
    public async Task<DailyStatusReportDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await dbContext.DailyStatusReports
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.CreatedByUser)
            .Include(x => x.SubmittedByUser)
            .Include(x => x.ReviewedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.TaskStatuses)
            .Include(x => x.PartIssues)
            .Include(x => x.MajorFindings)
            .Include(x => x.ImportHistories)
            .ThenInclude(x => x.UploadedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (report is null) throw new InvalidOperationException("Report not found");

        return ToDto(report);
    }

    public async Task<DailyStatusReportDto[]> ListAsync(int pageNumber = 1, int pageSize = 20, string? status = null, Guid? sectionId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.DailyStatusReports
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DailyStatusReportStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (sectionId.HasValue)
        {
            query = query.Where(x => x.SectionId == sectionId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.ReportDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.ReportDate <= endDate.Value);
        }

        var reports = await query
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.CreatedByUser)
            .OrderByDescending(x => x.ReportDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return reports.Select(ToDto).ToArray();
    }

    public async Task<DailyStatusReportDto> CreateAsync(CreateDailyStatusReportDto dto, CancellationToken cancellationToken = default)
    {
        var report = new DomainDailyStatusReport
        {
            Id = Guid.NewGuid(),
            ReportNumber = GenerateReportNumber(),
            AircraftRegistration = dto.AircraftRegistration,
            AircraftType = dto.AircraftType,
            Fleet = dto.Fleet,
            MaintenanceVisit = dto.MaintenanceVisit,
            CheckType = dto.CheckType,
            ReportDate = dto.ReportDate,
            Status = DailyStatusReportStatus.Draft,
            SectionId = dto.SectionId,
            HangarId = dto.HangarId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.DailyStatusReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(report.Id, cancellationToken);
    }

    public async Task<DailyStatusReportDto> UpdateAsync(Guid id, UpdateDailyStatusReportDto dto, CancellationToken cancellationToken = default)
    {
        var report = await dbContext.DailyStatusReports.FindAsync([id], cancellationToken);
        if (report is null) throw new InvalidOperationException("Report not found");

        if (dto.AircraftRegistration is not null) report.AircraftRegistration = dto.AircraftRegistration;
        if (dto.AircraftType is not null) report.AircraftType = dto.AircraftType;
        if (dto.Fleet is not null) report.Fleet = dto.Fleet;
        if (dto.MaintenanceVisit is not null) report.MaintenanceVisit = dto.MaintenanceVisit;
        if (dto.CheckType is not null) report.CheckType = dto.CheckType;
        if (dto.ReportDate.HasValue) report.ReportDate = dto.ReportDate.Value;
        if (dto.HangarId.HasValue) report.HangarId = dto.HangarId;

        report.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(report.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await dbContext.DailyStatusReports.FindAsync([id], cancellationToken);
        if (report is null) throw new InvalidOperationException("Report not found");

        dbContext.DailyStatusReports.Remove(report);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DailyStatusReportDto> SubmitAsync(Guid id, ReportDecisionDto dto, CancellationToken cancellationToken = default)
    {
        var report = await dbContext.DailyStatusReports.FindAsync([id], cancellationToken);
        if (report is null) throw new InvalidOperationException("Report not found");

        report.Status = DailyStatusReportStatus.Submitted;
        report.SubmittedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(report.Id, cancellationToken);
    }

    public async Task<DailyStatusReportDto> ReviewAsync(Guid id, ReportDecisionDto dto, CancellationToken cancellationToken = default)
    {
        var report = await dbContext.DailyStatusReports.FindAsync([id], cancellationToken);
        if (report is null) throw new InvalidOperationException("Report not found");

        report.Status = DailyStatusReportStatus.UnderReview;
        report.ReviewedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(report.Id, cancellationToken);
    }

    public async Task<DailyStatusReportDto> ApproveAsync(Guid id, ReportDecisionDto dto, CancellationToken cancellationToken = default)
    {
        var report = await dbContext.DailyStatusReports.FindAsync([id], cancellationToken);
        if (report is null) throw new InvalidOperationException("Report not found");

        report.Status = DailyStatusReportStatus.Approved;
        report.ApprovedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(report.Id, cancellationToken);
    }

    public async Task<DailyStatusReportDto> RejectAsync(Guid id, ReportDecisionDto dto, CancellationToken cancellationToken = default)
    {
        var report = await dbContext.DailyStatusReports.FindAsync([id], cancellationToken);
        if (report is null) throw new InvalidOperationException("Report not found");

        report.Status = DailyStatusReportStatus.Rejected;
        report.RejectionReason = dto.RejectionReason;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(report.Id, cancellationToken);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var reports = await dbContext.DailyStatusReports
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var taskStatuses = await dbContext.TaskStatuses
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var partIssues = await dbContext.PartIssues
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var findings = await dbContext.MajorFindings
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            TotalReports = reports.Count,
            DraftReports = reports.Count(x => x.Status == DailyStatusReportStatus.Draft),
            SubmittedReports = reports.Count(x => x.Status == DailyStatusReportStatus.Submitted),
            ApprovedReports = reports.Count(x => x.Status == DailyStatusReportStatus.Approved),
            PendingReview = reports.Count(x => x.Status == DailyStatusReportStatus.UnderReview),
            TotalTasks = taskStatuses.Count,
            CompletedTasks = taskStatuses.Count(x => x.Status == TaskStatusEnum.Complete),
            InWorkTasks = taskStatuses.Count(x => x.Status == TaskStatusEnum.InWork),
            TotalPartIssues = partIssues.Count,
            CriticalParts = partIssues.Count(x => x.IssueType == PartIssueType.Critical),
            TotalFindings = findings.Count,
            OpenFindings = findings.Count(x => x.Status == FindingStatus.Open),
            CriticalFindings = findings.Count(x => x.Severity == FindingSeverity.Critical)
        };
    }

    public async Task<AnalyticsDto> GetAnalyticsAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var taskStatuses = await dbContext.TaskStatuses
            .AsNoTracking()
            .Where(x => x.DailyStatusReportId == reportId)
            .ToListAsync(cancellationToken);

        var partIssues = await dbContext.PartIssues
            .AsNoTracking()
            .Where(x => x.DailyStatusReportId == reportId)
            .ToListAsync(cancellationToken);

        var findings = await dbContext.MajorFindings
            .AsNoTracking()
            .Where(x => x.DailyStatusReportId == reportId)
            .ToListAsync(cancellationToken);

        var taskStatusDistribution = taskStatuses
            .GroupBy(x => x.Status)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var phaseDistribution = taskStatuses
            .GroupBy(x => x.Phase)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var completionPercentage = taskStatuses.Count > 0
            ? (double)taskStatuses.Count(x => x.Status == TaskStatusEnum.Complete) / taskStatuses.Count * 100
            : 0;

        var partIssueDistribution = partIssues
            .GroupBy(x => x.IssueType)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var findingSeverityDistribution = findings
            .GroupBy(x => x.Severity)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var findingStatusDistribution = findings
            .GroupBy(x => x.Status)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        return new AnalyticsDto
        {
            TaskStatusDistribution = taskStatusDistribution,
            PhaseDistribution = phaseDistribution,
            CompletionPercentage = completionPercentage,
            PartIssueDistribution = partIssueDistribution,
            FindingSeverityDistribution = findingSeverityDistribution,
            FindingStatusDistribution = findingStatusDistribution
        };
    }

    public async Task<PhaseProgressDto[]> GetPhaseProgressAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var taskStatuses = await dbContext.TaskStatuses
            .AsNoTracking()
            .Where(x => x.DailyStatusReportId == reportId)
            .ToListAsync(cancellationToken);

        var statuses = Enum.GetValues<TaskStatusEnum>();
        var phases = Enum.GetValues<PhaseEnum>();

        var result = new List<PhaseProgressDto>();

        foreach (var status in statuses)
        {
            var dto = new PhaseProgressDto
            {
                Status = status,
                Incoming = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.Incoming),
                OpenUp = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.OpenUp),
                Clean = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.Clean),
                FunAndOpr = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.FunAndOpr),
                CDP = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.CDP),
                EO_AD = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.EO_AD),
                Dis_Rep = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.Dis_Rep),
                LubAndSer = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.LubAndSer),
                DBC = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.DBC),
                Remarks = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.Remarks),
                CloseUp = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.CloseUp),
                Final = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.Final),
                DeHan = taskStatuses.Count(x => x.Status == status && x.Phase == PhaseEnum.DeHangaring),
                Total = taskStatuses.Count(x => x.Status == status)
            };
            result.Add(dto);
        }

        return result.ToArray();
    }

    public async Task<OverallStatusDto[]> GetOverallStatusAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var taskStatuses = await dbContext.TaskStatuses
            .AsNoTracking()
            .Where(x => x.DailyStatusReportId == reportId)
            .ToListAsync(cancellationToken);

        var result = new List<OverallStatusDto>
        {
            new()
            {
                TypeOfTask = "Routine",
                Complete = taskStatuses.Count(x => x.TaskType.Equals("Routine", StringComparison.OrdinalIgnoreCase) && x.Status == TaskStatusEnum.Complete),
                Unassign = taskStatuses.Count(x => x.TaskType.Equals("Routine", StringComparison.OrdinalIgnoreCase) && x.Status == TaskStatusEnum.Unassign),
                InWork = taskStatuses.Count(x => x.TaskType.Equals("Routine", StringComparison.OrdinalIgnoreCase) && x.Status == TaskStatusEnum.InWork),
                Pause = taskStatuses.Count(x => x.TaskType.Equals("Routine", StringComparison.OrdinalIgnoreCase) && x.Status == TaskStatusEnum.Pause),
                Active = taskStatuses.Count(x => x.TaskType.Equals("Routine", StringComparison.OrdinalIgnoreCase) && x.Status == TaskStatusEnum.Active),
                Total = taskStatuses.Count(x => x.TaskType.Equals("Routine", StringComparison.OrdinalIgnoreCase))
            },
            new()
            {
                TypeOfTask = "Non-Routine",
                Complete = taskStatuses.Count(x => x.TaskType.Equals("Non-Routine", StringComparison.OrdinalIgnoreCase) && x.Status == TaskStatusEnum.Complete),
                Unassign = taskStatuses.Count(x => x.TaskType.Equals("Non-Routine", StringComparison.OrdinalIgnoreCase) && x.Status == TaskStatusEnum.Unassign),
                InWork = taskStatuses.Count(x => x.TaskType.Equals("Non-Routine", StringComparison.OrdinalIgnoreCase) && x.Status == TaskStatusEnum.InWork),
                Pause = taskStatuses.Count(x => x.TaskType.Equals("Non-Routine", StringComparison.OrdinalIgnoreCase) && x.Status == TaskStatusEnum.Pause),
                Active = taskStatuses.Count(x => x.TaskType.Equals("Non-Routine", StringComparison.OrdinalIgnoreCase) && x.Status == TaskStatusEnum.Active),
                Total = taskStatuses.Count(x => x.TaskType.Equals("Non-Routine", StringComparison.OrdinalIgnoreCase))
            }
        };

        return result.ToArray();
    }

    public async Task<ImportValidationResultDto> ImportExcelAsync(ImportExcelDto dto, CancellationToken cancellationToken = default)
    {
        var report = await dbContext.DailyStatusReports.FindAsync([dto.ReportId], cancellationToken);
        if (report is null) throw new InvalidOperationException("Report not found");

        var errors = new List<string>();
        var warnings = new List<string>();
        var successfulRecords = 0;
        var failedRecords = 0;

        // Parse Excel file (simplified implementation - in production use EPPlus or ClosedXML)
        try
        {
            // For now, this is a placeholder for Excel parsing
            // In production, integrate with EPPlus or ClosedXML library
            // This would parse the Excel file and validate columns

            var importHistory = new ImportHistory
            {
                Id = Guid.NewGuid(),
                DailyStatusReportId = dto.ReportId,
                ImportType = dto.ImportType,
                FileName = dto.FileName,
                UploadedByUserId = Guid.Empty, // Would be set from current user context
                UploadDate = DateTime.UtcNow,
                RecordCount = successfulRecords,
                ImportStatus = ImportStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                ColumnMapping = dto.ColumnMapping
            };

            dbContext.ImportHistories.Add(importHistory);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new ImportValidationResultDto
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray(),
                SuccessfulRecords = successfulRecords,
                FailedRecords = failedRecords
            };
        }
        catch (Exception ex)
        {
            errors.Add($"Import failed: {ex.Message}");
            return new ImportValidationResultDto
            {
                IsValid = false,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray(),
                SuccessfulRecords = 0,
                FailedRecords = 0
            };
        }
    }

    public async Task RollbackImportAsync(Guid importHistoryId, CancellationToken cancellationToken = default)
    {
        var importHistory = await dbContext.ImportHistories.FindAsync([importHistoryId], cancellationToken);
        if (importHistory is null) throw new InvalidOperationException("Import history not found");

        // Rollback logic would delete associated records based on import type
        // This is a simplified implementation
        importHistory.ImportStatus = ImportStatus.RolledBack;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<byte[]> ExportReportAsync(Guid reportId, string format, CancellationToken cancellationToken = default)
    {
        var report = await GetByIdAsync(reportId, cancellationToken);

        // Placeholder for export functionality
        // In production, integrate with PDF generation library (e.g., iTextSharp, QuestPDF)
        // or Excel export library (EPPlus, ClosedXML)
        return Array.Empty<byte>();
    }

    private static DailyStatusReportDto ToDto(DomainDailyStatusReport report)
    {
        return new DailyStatusReportDto
        {
            Id = report.Id,
            ReportNumber = report.ReportNumber,
            AircraftRegistration = report.AircraftRegistration,
            AircraftType = report.AircraftType,
            Fleet = report.Fleet,
            MaintenanceVisit = report.MaintenanceVisit,
            CheckType = report.CheckType,
            ReportDate = report.ReportDate,
            Status = report.Status,
            SectionId = report.SectionId,
            HangarId = report.HangarId,
            SectionName = report.Section?.Name,
            HangarName = report.Hangar?.Name,
            CreatedByName = report.CreatedByUser?.FullName,
            CreatedAt = report.CreatedAt.DateTime,
            LastUpdatedAt = report.UpdatedAt?.DateTime,
            SubmittedByName = report.SubmittedByUser?.FullName,
            SubmittedAt = report.SubmittedAt,
            ReviewedByName = report.ReviewedByUser?.FullName,
            ReviewedAt = report.ReviewedAt,
            ApprovedByName = report.ApprovedByUser?.FullName,
            ApprovedAt = report.ApprovedAt,
            RejectionReason = report.RejectionReason,
            TaskStatuses = report.TaskStatuses.Select(ToTaskStatusDto).ToArray(),
            PartIssues = report.PartIssues.Select(ToPartIssueDto).ToArray(),
            MajorFindings = report.MajorFindings.Select(ToMajorFindingDto).ToArray(),
            ImportHistories = report.ImportHistories.Select(ToImportHistoryDto).ToArray()
        };
    }

    private static TaskStatusDto ToTaskStatusDto(DomainTaskStatus taskStatus)
    {
        return new TaskStatusDto
        {
            Id = taskStatus.Id,
            TaskName = taskStatus.TaskName,
            TaskId = taskStatus.TaskId,
            TaskType = taskStatus.TaskType,
            Phase = taskStatus.Phase,
            Status = taskStatus.Status,
            SerialNumber = taskStatus.SerialNumber
        };
    }

    private static PartIssueDto ToPartIssueDto(PartIssue partIssue)
    {
        return new PartIssueDto
        {
            Id = partIssue.Id,
            IssueType = partIssue.IssueType,
            ItemNumber = partIssue.ItemNumber,
            Task = partIssue.Task,
            PartNumber = partIssue.PartNumber,
            Description = partIssue.Description,
            RID = partIssue.RID,
            Quantity = partIssue.Quantity,
            DateRequested = partIssue.DateRequested,
            DateReceived = partIssue.DateReceived,
            DateRobbed = partIssue.DateRobbed,
            ClosedDate = partIssue.ClosedDate,
            PONumber = partIssue.PONumber,
            ResponsibleBuyer = partIssue.ResponsibleBuyer,
            Vendor = partIssue.Vendor,
            DonorAircraft = partIssue.DonorAircraft,
            RecipientAircraft = partIssue.RecipientAircraft,
            Status = partIssue.Status,
            Remark = partIssue.Remark,
            EDD = partIssue.EDD,
            Resolution = partIssue.Resolution,
            ClosedBy = partIssue.ClosedBy
        };
    }

    private static MajorFindingDto ToMajorFindingDto(MajorFinding finding)
    {
        return new MajorFindingDto
        {
            Id = finding.Id,
            FindingNumber = finding.FindingNumber,
            ATAChapter = finding.ATAChapter,
            Description = finding.Description,
            Severity = finding.Severity,
            Status = finding.Status,
            RaisedDate = finding.RaisedDate,
            Owner = finding.Owner,
            TargetClosureDate = finding.TargetClosureDate,
            ClosureDate = finding.ClosureDate,
            Remarks = finding.Remarks
        };
    }

    private static ImportHistoryDto ToImportHistoryDto(ImportHistory history)
    {
        return new ImportHistoryDto
        {
            Id = history.Id,
            ImportType = history.ImportType,
            FileName = history.FileName,
            UploadedByName = history.UploadedByUser?.FullName ?? string.Empty,
            UploadDate = history.UploadDate,
            RecordCount = history.RecordCount,
            ImportStatus = history.ImportStatus,
            ErrorMessage = history.ErrorMessage,
            CompletedAt = history.CompletedAt,
            ColumnMapping = history.ColumnMapping
        };
    }

    private static string GenerateReportNumber()
    {
        return $"DSR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
