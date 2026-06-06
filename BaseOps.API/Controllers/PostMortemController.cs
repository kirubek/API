using BaseOps.API.Models;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
public sealed class PostMortemController(BaseOpsDbContext dbContext) : ControllerBase
{
    [HttpGet("api/post-mortem-reports")]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] Guid? sectionId = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var query = ApplyScope(dbContext.PostMortemReports
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.CreatedByUser), currentUser);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PostMortemReportStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (sectionId.HasValue)
        {
            query = query.Where(x => x.SectionId == sectionId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var reports = await query
            .OrderByDescending(x => x.ScheduledIn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = reports.Select(ToReportDto).ToArray();
        return Ok(ApiResults.Page<object>(items, total, pageNumber, pageSize));
    }

    [HttpGet("api/post-mortem-reports/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await ApplyScope(dbContext.PostMortemReports
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.CreatedByUser)
            .Include(x => x.SubmittedByUser)
            .Include(x => x.ReviewedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.SlaRecords)
            .Include(x => x.CrsCompletions)
            .Include(x => x.TatRecords)
            .Include(x => x.PlanStabilityRecords)
            .Include(x => x.CarryOverTasks), currentUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return report is null ? NotFound() : Ok(ToReportDto(report));
    }

    [HttpPost("api/post-mortem-reports")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Create([FromBody] CreatePostMortemReportDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = new PostMortemReport
        {
            Id = Guid.NewGuid(),
            ReportNumber = GenerateReportNumber(),
            WorkPackageId = dto.WorkPackageId,
            WorkPackageDescription = dto.WorkPackageDescription,
            AircraftRegistration = dto.AircraftRegistration,
            AircraftType = dto.AircraftType,
            HangaringStatus = dto.HangaringStatus,
            DeHangaringStatus = dto.DeHangaringStatus,
            TatStatus = dto.TatStatus,
            CheckType = dto.CheckType,
            ScheduledIn = dto.ScheduledIn,
            ActualIn = dto.ActualIn,
            ScheduledOut = dto.ScheduledOut,
            ActualOut = dto.ActualOut,
            IncomingDeviationReason = dto.IncomingDeviationReason,
            DeviationReasonDeHangaring = dto.DeviationReasonDeHangaring,
            ScheduleTATHours = dto.ScheduleTATHours,
            ActualTATHours = dto.ActualTATHours,
            Remarks = dto.Remarks,
            Status = PostMortemReportStatus.Draft,
            SectionId = dto.SectionId,
            HangarId = dto.HangarId,
            CreatedBy = currentUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PostMortemReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = report.Id }, ToReportDto(report));
    }

    [HttpPut("api/post-mortem-reports/{id:guid}")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostMortemReportDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await ApplyScope(dbContext.PostMortemReports, currentUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (report is null) return NotFound();

        if (!CanEditReport(report, currentUser))
            return Forbid();

        if (dto.WorkPackageDescription is not null) report.WorkPackageDescription = dto.WorkPackageDescription;
        if (dto.HangaringStatus is not null) report.HangaringStatus = dto.HangaringStatus;
        if (dto.DeHangaringStatus is not null) report.DeHangaringStatus = dto.DeHangaringStatus;
        if (dto.TatStatus is not null) report.TatStatus = dto.TatStatus;
        if (dto.ActualIn is not null) report.ActualIn = dto.ActualIn.Value;
        if (dto.ActualOut is not null) report.ActualOut = dto.ActualOut.Value;
        if (dto.ScheduleTATHours is not null) report.ScheduleTATHours = dto.ScheduleTATHours.Value;
        if (dto.ActualTATHours is not null) report.ActualTATHours = dto.ActualTATHours.Value;
        if (dto.Remarks is not null) report.Remarks = dto.Remarks;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToReportDto(report));
    }

    [HttpPost("api/post-mortem-reports/{id:guid}/submit")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await ApplyScope(dbContext.PostMortemReports, currentUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (report is null) return NotFound();

        if (!CanEditReport(report, currentUser))
            return Forbid();

        if (report.Status != PostMortemReportStatus.Draft)
            return BadRequest("Only draft reports can be submitted");

        report.Status = PostMortemReportStatus.Submitted;
        report.SubmittedByUserId = currentUser.Id;
        report.SubmittedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToReportDto(report));
    }

    [HttpPost("api/post-mortem-reports/{id:guid}/approve")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] PostMortemApproveRejectDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        if (!IsManagerOrAbove(currentUser))
            return Forbid();

        var report = await dbContext.PostMortemReports
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (report is null) return NotFound();

        if (report.Status != PostMortemReportStatus.Submitted && report.Status != PostMortemReportStatus.UnderReview)
            return BadRequest("Only submitted or under review reports can be approved");

        report.Status = PostMortemReportStatus.Approved;
        report.ApprovedByUserId = currentUser.Id;
        report.ApprovedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToReportDto(report));
    }

    [HttpPost("api/post-mortem-reports/{id:guid}/reject")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] PostMortemApproveRejectDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        if (!IsManagerOrAbove(currentUser))
            return Forbid();

        var report = await dbContext.PostMortemReports
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (report is null) return NotFound();

        if (report.Status != PostMortemReportStatus.Submitted && report.Status != PostMortemReportStatus.UnderReview)
            return BadRequest("Only submitted or under review reports can be rejected");

        report.Status = PostMortemReportStatus.Rejected;
        report.RejectionReason = dto.Reason;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToReportDto(report));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return null;
        return await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
    }

    private static IQueryable<PostMortemReport> ApplyScope(IQueryable<PostMortemReport> query, ApplicationUser currentUser)
    {
        return currentUser.Role switch
        {
            UserRole.Director or UserRole.SystemAdmin => query,
            UserRole.Manager when currentUser.SectionId.HasValue => query.Where(x => x.SectionId == currentUser.SectionId.Value),
            UserRole.TeamLeader when currentUser.HangarId.HasValue => query.Where(x => x.HangarId == currentUser.HangarId.Value),
            UserRole.Employee => query.Where(x => x.CreatedByUserId == currentUser.Id),
            _ => query.Where(_ => false)
        };
    }

    private static bool CanEditReport(PostMortemReport report, ApplicationUser currentUser)
    {
        return currentUser.Role switch
        {
            UserRole.Director or UserRole.SystemAdmin => true,
            UserRole.Manager when currentUser.SectionId == report.SectionId => true,
            UserRole.TeamLeader when currentUser.HangarId == report.HangarId => true,
            UserRole.Employee when report.CreatedByUserId == currentUser.Id => true,
            _ => false
        };
    }

    private static bool IsManagerOrAbove(ApplicationUser currentUser)
    {
        return currentUser.Role is UserRole.Manager or UserRole.Director or UserRole.SystemAdmin;
    }

    private static string GenerateReportNumber()
    {
        return $"PM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private static object ToReportDto(PostMortemReport report)
    {
        var slaRecords = report.SlaRecords.Select(x => new
        {
            id = x.Id,
            slaType = x.SlaType,
            target = x.Target,
            actual = x.Actual,
            variance = x.Variance,
            status = x.Status,
            remarks = x.Remarks
        }).ToArray();

        var crsCompletions = report.CrsCompletions.Select(x => new
        {
            id = x.Id,
            crsNumber = x.CrsNumber,
            description = x.Description,
            status = x.Status,
            completedDate = x.CompletedDate,
            remarks = x.Remarks
        }).ToArray();

        var tatRecords = report.TatRecords.Select(x => new
        {
            id = x.Id,
            taskDescription = x.TaskDescription,
            plannedHours = x.PlannedHours,
            actualHours = x.ActualHours,
            variance = x.Variance,
            status = x.Status,
            delayReason = x.DelayReason,
            hangaringNumber = x.HangaringNumber,
            hangaringAC = x.HangaringAC,
            dehangaringNumber = x.DehangaringNumber,
            dehangaringAC = x.DehangaringAC,
            remarks = x.Remarks
        }).ToArray();

        var planStabilityRecords = report.PlanStabilityRecords.Select(x => new
        {
            id = x.Id,
            planVersion = x.PlanVersion,
            effectiveDate = x.EffectiveDate,
            changeCount = x.ChangeCount,
            changeReason = x.ChangeReason,
            remarks = x.Remarks
        }).ToArray();

        var carryOverTasks = report.CarryOverTasks.Select(x => new
        {
            id = x.Id,
            description = x.Description,
            aircraftRegistration = x.AircraftRegistration,
            aircraftType = x.AircraftType,
            taskType = x.TaskType.ToString(),
            deferralReason = x.DeferralReason,
            status = x.Status.ToString(),
            targetDate = x.TargetDate,
            assignedToUserId = x.AssignedToUserId,
            remarks = x.Remarks
        }).ToArray();

        return new
        {
            id = report.Id,
            reportNumber = report.ReportNumber,
            workPackageId = report.WorkPackageId,
            workPackageDescription = report.WorkPackageDescription,
            aircraftRegistration = report.AircraftRegistration,
            aircraftType = report.AircraftType,
            hangaringStatus = report.HangaringStatus,
            deHangaringStatus = report.DeHangaringStatus,
            tatStatus = report.TatStatus,
            checkType = report.CheckType.ToString(),
            scheduledIn = report.ScheduledIn,
            actualIn = report.ActualIn,
            scheduledOut = report.ScheduledOut,
            actualOut = report.ActualOut,
            incomingDeviationReason = report.IncomingDeviationReason,
            deviationReasonDeHangaring = report.DeviationReasonDeHangaring,
            scheduleTATHours = report.ScheduleTATHours,
            actualTATHours = report.ActualTATHours,
            remarks = report.Remarks,
            status = report.Status.ToString(),
            sectionId = report.SectionId,
            section = report.Section?.Name,
            hangarId = report.HangarId,
            hangar = report.Hangar?.Name,
            createdByUserId = report.CreatedByUserId,
            createdBy = report.CreatedByUser != null ? new { id = report.CreatedByUser.Id, fullName = report.CreatedByUser.FullName, email = report.CreatedByUser.Email } : null,
            submittedByUserId = report.SubmittedByUserId,
            submittedAt = report.SubmittedAt,
            submittedBy = report.SubmittedByUser != null ? new { id = report.SubmittedByUser.Id, fullName = report.SubmittedByUser.FullName } : null,
            reviewedByUserId = report.ReviewedByUserId,
            reviewedAt = report.ReviewedAt,
            reviewedBy = report.ReviewedByUser != null ? new { id = report.ReviewedByUser.Id, fullName = report.ReviewedByUser.FullName } : null,
            approvedByUserId = report.ApprovedByUserId,
            approvedAt = report.ApprovedAt,
            approvedBy = report.ApprovedByUser != null ? new { id = report.ApprovedByUser.Id, fullName = report.ApprovedByUser.FullName } : null,
            rejectionReason = report.RejectionReason,
            createdAt = report.CreatedAt,
            updatedAt = report.UpdatedAt,
            slaRecords,
            crsCompletions,
            tatRecords,
            planStabilityRecords,
            carryOverTasks
        };
    }

    [HttpGet("api/post-mortem-reports/dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var query = ApplyScope(dbContext.PostMortemReports.AsNoTracking(), currentUser);

        if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var parsedStart))
        {
            query = query.Where(x => x.ScheduledIn >= parsedStart);
        }

        if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
        {
            query = query.Where(x => x.ScheduledIn <= parsedEnd);
        }

        var total = await query.CountAsync(cancellationToken);
        var completed = await query.Where(x => x.Status == PostMortemReportStatus.Approved).CountAsync(cancellationToken);
        var pending = await query.Where(x => x.Status == PostMortemReportStatus.Submitted).CountAsync(cancellationToken);
        var draft = await query.Where(x => x.Status == PostMortemReportStatus.Draft).CountAsync(cancellationToken);

        return Ok(new
        {
            total,
            completed,
            pending,
            draft,
            slaComplianceRate = total > 0 ? (double)completed / total * 100 : 0
        });
    }

    [HttpGet("api/post-mortem-reports/list")]
    public async Task<IActionResult> GetList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var query = ApplyScope(dbContext.PostMortemReports
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar), currentUser);

        var total = await query.CountAsync(cancellationToken);
        var reports = await query
            .OrderByDescending(x => x.ScheduledIn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.ReportNumber,
                x.WorkPackageId,
                x.WorkPackageDescription,
                x.AircraftRegistration,
                x.AircraftType,
                x.Status,
                x.ScheduledIn,
                x.ScheduledOut,
                x.ActualIn,
                x.ActualOut,
                x.ScheduleTATHours,
                x.ActualTATHours,
                section = x.Section != null ? new { x.Section.Id, x.Section.Name } : null,
                hangar = x.Hangar != null ? new { x.Hangar.Id, x.Hangar.Name } : null
            })
            .ToListAsync(cancellationToken);

        return Ok(new { items = reports, total, pageNumber, pageSize });
    }

    [HttpGet("api/post-mortem-reports/analytics")]
    public async Task<IActionResult> GetAnalytics([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var query = ApplyScope(dbContext.PostMortemReports.AsNoTracking(), currentUser);

        if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var parsedStart))
        {
            query = query.Where(x => x.ScheduledIn >= parsedStart);
        }

        if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
        {
            query = query.Where(x => x.ScheduledIn <= parsedEnd);
        }

        var reports = await query.ToListAsync(cancellationToken);

        return Ok(new
        {
            totalReports = reports.Count,
            approvedReports = reports.Count(x => x.Status == PostMortemReportStatus.Approved),
            averageTATVariance = reports.Any(x => x.ScheduleTATHours > 0) 
                ? reports.Where(x => x.ScheduleTATHours > 0).Average(x => x.ActualTATHours - x.ScheduleTATHours) 
                : 0,
            hangaringDeviations = reports.Count(x => !string.IsNullOrWhiteSpace(x.IncomingDeviationReason)),
            dehangaringDeviations = reports.Count(x => !string.IsNullOrWhiteSpace(x.DeviationReasonDeHangaring))
        });
    }

    [HttpGet("api/post-mortem-reports/charts")]
    public async Task<IActionResult> GetCharts([FromQuery] string? chartType = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var query = ApplyScope(dbContext.PostMortemReports.AsNoTracking(), currentUser);
        var reports = await query.ToListAsync(cancellationToken);

        var data = new
        {
            statusDistribution = reports.GroupBy(x => x.Status)
                .Select(g => new { status = g.Key.ToString(), count = g.Count() })
                .ToList(),
            tatVarianceTrend = reports.Where(x => x.ScheduleTATHours > 0)
                .Select(x => new
                {
                    x.ReportNumber,
                    variance = x.ActualTATHours - x.ScheduleTATHours,
                    x.ScheduledIn
                })
                .OrderBy(x => x.ScheduledIn)
                .ToList()
        };

        return Ok(data);
    }
}

public record CreatePostMortemReportDto(
    string WorkPackageId,
    string WorkPackageDescription,
    string AircraftRegistration,
    string AircraftType,
    string HangaringStatus,
    string DeHangaringStatus,
    string TatStatus,
    PostMortemCheckType CheckType,
    DateTime ScheduledIn,
    DateTime ActualIn,
    DateTime ScheduledOut,
    DateTime ActualOut,
    string IncomingDeviationReason,
    string DeviationReasonDeHangaring,
    decimal ScheduleTATHours,
    decimal ActualTATHours,
    string Remarks,
    Guid SectionId,
    Guid? HangarId
);

public record UpdatePostMortemReportDto(
    string? WorkPackageDescription,
    string? HangaringStatus,
    string? DeHangaringStatus,
    string? TatStatus,
    DateTime? ActualIn,
    DateTime? ActualOut,
    decimal? ScheduleTATHours,
    decimal? ActualTATHours,
    string? Remarks
);

public record PostMortemApproveRejectDto(string? Comments, string? Reason);
