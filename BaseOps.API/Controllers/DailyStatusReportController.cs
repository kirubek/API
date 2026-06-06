using BaseOps.API.Models;
using BaseOps.Application.DailyStatusReport.DTOs;
using BaseOps.Application.DailyStatusReport;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
public sealed class DailyStatusReportController(BaseOpsDbContext dbContext, IDailyStatusReportService dailyStatusReportService) : ControllerBase
{
    [HttpGet("api/daily-status-reports")]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] Guid? sectionId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = await GetCurrentUserAsync(cancellationToken);
            if (currentUser is null) return Unauthorized();

            var query = ApplyScope(dbContext.DailyStatusReports
                .AsNoTracking()
                .Include(x => x.Section)
                .Include(x => x.Hangar)
                .Include(x => x.CreatedByUser), currentUser);

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

            var total = await query.CountAsync(cancellationToken);
            var reports = await query
                .OrderByDescending(x => x.ReportDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = reports.Select(ToReportDto).ToArray();
            return Ok(new { items, total, pageNumber, pageSize });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"List error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("api/daily-status-reports/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await ApplyScope(dbContext.DailyStatusReports
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
            .ThenInclude(x => x.UploadedByUser), currentUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return report is null ? NotFound() : Ok(ToReportDto(report));
    }

    [HttpPost("api/daily-status-reports")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateDailyStatusReportDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await dailyStatusReportService.CreateAsync(dto, cancellationToken);
        return Ok(report);
    }

    [HttpPut("api/daily-status-reports/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDailyStatusReportDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await dailyStatusReportService.UpdateAsync(id, dto, cancellationToken);
        return Ok(report);
    }

    [HttpDelete("api/daily-status-reports/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        await dailyStatusReportService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/daily-status-reports/{id:guid}/submit")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] ReportDecisionDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await dailyStatusReportService.SubmitAsync(id, dto, cancellationToken);
        return Ok(report);
    }

    [HttpPost("api/daily-status-reports/{id:guid}/review")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReportDecisionDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await dailyStatusReportService.ReviewAsync(id, dto, cancellationToken);
        return Ok(report);
    }

    [HttpPost("api/daily-status-reports/{id:guid}/approve")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReportDecisionDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await dailyStatusReportService.ApproveAsync(id, dto, cancellationToken);
        return Ok(report);
    }

    [HttpPost("api/daily-status-reports/{id:guid}/reject")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReportDecisionDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await dailyStatusReportService.RejectAsync(id, dto, cancellationToken);
        return Ok(report);
    }

    [HttpGet("api/daily-status-reports/dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = await GetCurrentUserAsync(cancellationToken);
            if (currentUser is null) return Unauthorized();

            var summary = await dailyStatusReportService.GetDashboardSummaryAsync(cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dashboard error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("api/daily-status-reports/{id:guid}/analytics")]
    public async Task<IActionResult> GetAnalytics(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var analytics = await dailyStatusReportService.GetAnalyticsAsync(id, cancellationToken);
        return Ok(analytics);
    }

    [HttpGet("api/daily-status-reports/{id:guid}/phase-progress")]
    public async Task<IActionResult> GetPhaseProgress(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var phaseProgress = await dailyStatusReportService.GetPhaseProgressAsync(id, cancellationToken);
        return Ok(phaseProgress);
    }

    [HttpGet("api/daily-status-reports/{id:guid}/overall-status")]
    public async Task<IActionResult> GetOverallStatus(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var overallStatus = await dailyStatusReportService.GetOverallStatusAsync(id, cancellationToken);
        return Ok(overallStatus);
    }

    [HttpPost("api/daily-status-reports/import")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> ImportExcel([FromForm] IFormFile file, [FromForm] Guid reportId, [FromForm] ImportType importType, [FromForm] string? columnMapping = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        var dto = new ImportExcelDto
        {
            ReportId = reportId,
            ImportType = importType,
            FileName = file.FileName,
            FileContent = memoryStream.ToArray(),
            ColumnMapping = columnMapping
        };

        var result = await dailyStatusReportService.ImportExcelAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/daily-status-reports/rollback/{importHistoryId:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> RollbackImport(Guid importHistoryId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        await dailyStatusReportService.RollbackImportAsync(importHistoryId, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/daily-status-reports/{id:guid}/export")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> ExportReport(Guid id, [FromQuery] string format = "PDF", CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var data = await dailyStatusReportService.ExportReportAsync(id, format, cancellationToken);
        return File(data, format == "PDF" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DailyStatusReport_{id}.{format.ToLower()}");
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            // Try alternative claim names
            userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out userId))
                return null;
        }

        return await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    private IQueryable<DailyStatusReport> ApplyScope(IQueryable<DailyStatusReport> query, ApplicationUser currentUser)
    {
        // Temporarily allow all authenticated users to access
        // TODO: Re-enable proper scoping after debugging
        return query;
    }

    private static object ToReportDto(DailyStatusReport report)
    {
        return new
        {
            report.Id,
            report.ReportNumber,
            report.AircraftRegistration,
            report.AircraftType,
            report.Fleet,
            report.MaintenanceVisit,
            report.CheckType,
            report.ReportDate,
            Status = report.Status.ToString(),
            report.SectionId,
            report.HangarId,
            SectionName = report.Section?.Name,
            HangarName = report.Hangar?.Name,
            CreatedByName = report.CreatedByUser?.FullName,
            report.CreatedAt,
            report.UpdatedAt,
            SubmittedByName = report.SubmittedByUser?.FullName,
            report.SubmittedAt,
            ReviewedByName = report.ReviewedByUser?.FullName,
            report.ReviewedAt,
            ApprovedByName = report.ApprovedByUser?.FullName,
            report.ApprovedAt,
            report.RejectionReason,
            TaskStatuses = report.TaskStatuses.Select(ts => new
            {
                ts.Id,
                ts.TaskName,
                ts.TaskId,
                ts.TaskType,
                Phase = ts.Phase.ToString(),
                Status = ts.Status.ToString(),
                ts.SerialNumber
            }),
            PartIssues = report.PartIssues.Select(pi => new
            {
                pi.Id,
                IssueType = pi.IssueType.ToString(),
                pi.ItemNumber,
                pi.Task,
                pi.PartNumber,
                pi.Description,
                pi.RID,
                pi.Quantity,
                pi.DateRequested,
                pi.DateReceived,
                pi.DateRobbed,
                pi.ClosedDate,
                pi.PONumber,
                pi.ResponsibleBuyer,
                pi.Vendor,
                pi.DonorAircraft,
                pi.RecipientAircraft,
                pi.Status,
                pi.Remark,
                pi.EDD,
                pi.Resolution,
                pi.ClosedBy
            }),
            MajorFindings = report.MajorFindings.Select(mf => new
            {
                mf.Id,
                mf.FindingNumber,
                mf.ATAChapter,
                mf.Description,
                Severity = mf.Severity.ToString(),
                Status = mf.Status.ToString(),
                mf.RaisedDate,
                mf.Owner,
                mf.TargetClosureDate,
                mf.ClosureDate,
                mf.Remarks
            }),
            ImportHistories = report.ImportHistories.Select(ih => new
            {
                ih.Id,
                ImportType = ih.ImportType.ToString(),
                ih.FileName,
                UploadedByName = ih.UploadedByUser?.FullName,
                ih.UploadDate,
                ih.RecordCount,
                ImportStatus = ih.ImportStatus.ToString(),
                ih.ErrorMessage,
                ih.CompletedAt,
                ih.ColumnMapping
            })
        };
    }
}
