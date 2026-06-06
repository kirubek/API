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
public sealed class CarryOverController(BaseOpsDbContext dbContext) : ControllerBase
{
    [HttpGet("api/carry-over/reports")]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] Guid? sectionId = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var query = ApplyScope(dbContext.CarryOverReports
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.AssignedToUser)
            .Include(x => x.Tasks), currentUser);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CarryOverReportStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (sectionId.HasValue)
        {
            query = query.Where(x => x.SectionId == sectionId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var reports = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = reports.Select(ToReportDto).ToArray();
        return Ok(ApiResults.Page<object>(items, total, pageNumber, pageSize));
    }

    [HttpGet("api/carry-over/reports/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await ApplyScope(dbContext.CarryOverReports
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.AssignedToUser)
            .Include(x => x.SubmittedByUser)
            .Include(x => x.ReviewedByUser)
            .Include(x => x.FinalizedByUser)
            .Include(x => x.Tasks)
            .Include(x => x.Reviews), currentUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return report is null ? NotFound() : Ok(ToReportDto(report));
    }

    [HttpPost("api/carry-over/reports")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateCarryOverReportDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = new CarryOverReport
        {
            Id = Guid.NewGuid(),
            ReportNumber = GenerateReportNumber(),
            Title = dto.Title,
            Description = dto.Description,
            AircraftRegistration = dto.AircraftRegistration,
            AircraftType = dto.AircraftType,
            ProjectType = dto.ProjectType,
            Priority = dto.Priority,
            Status = CarryOverReportStatus.Draft,
            DueDate = dto.DueDate,
            SectionId = dto.SectionId,
            HangarId = dto.HangarId,
            AssignedToUserId = dto.AssignedToUserId,
            CarryOverTasks = dto.Tasks.Count,
            CarryOverPercentage = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.Id
        };

        foreach (var taskDto in dto.Tasks)
        {
            report.Tasks.Add(new CarryOverTask
            {
                Id = Guid.NewGuid(),
                CarryOverReportId = report.Id,
                Title = taskDto.Title,
                Description = taskDto.Description,
                Category = taskDto.Category,
                Priority = taskDto.Priority,
                Status = CarryOverTaskStatus.Open,
                TaskType = taskDto.TaskType,
                DeferralReason = taskDto.DeferralReason,
                DeferralDetails = taskDto.DeferralDetails,
                DeferredTaskOrigin = taskDto.DeferredTaskOrigin,
                AssignedToUserId = taskDto.AssignedToUserId,
                EstimatedHours = taskDto.EstimatedHours,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser.Id
            });
        }

        dbContext.CarryOverReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = report.Id }, ToReportDto(report));
    }

    [HttpPut("api/carry-over/reports/{id:guid}")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCarryOverReportDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await ApplyScope(dbContext.CarryOverReports
            .Include(x => x.Tasks), currentUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (report is null) return NotFound();

        if (!CanEditReport(report, currentUser))
            return Forbid();

        if (dto.Title is not null) report.Title = dto.Title;
        if (dto.Description is not null) report.Description = dto.Description;
        if (dto.Priority is not null) report.Priority = dto.Priority;
        if (dto.DueDate is not null) report.DueDate = dto.DueDate.Value;
        if (dto.AssignedToUserId is not null) report.AssignedToUserId = dto.AssignedToUserId;
        if (dto.Remarks is not null) report.Remarks = dto.Remarks;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToReportDto(report));
    }

    [HttpPost("api/carry-over/reports/{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await ApplyScope(dbContext.CarryOverReports, currentUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (report is null) return NotFound();

        if (!CanEditReport(report, currentUser))
            return Forbid();

        if (report.Status != CarryOverReportStatus.Draft)
            return BadRequest("Only draft reports can be submitted");

        report.Status = CarryOverReportStatus.Submitted;
        report.SubmittedByUserId = currentUser.Id;
        report.SubmittedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToReportDto(report));
    }

    [HttpPost("api/carry-over/reports/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveRejectDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        if (!IsManagerOrAbove(currentUser))
            return Forbid();

        var report = await dbContext.CarryOverReports
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (report is null) return NotFound();

        if (report.Status != CarryOverReportStatus.Submitted && report.Status != CarryOverReportStatus.Reviewed)
            return BadRequest("Only submitted or reviewed reports can be approved");

        report.Status = CarryOverReportStatus.Finalized;
        report.FinalizedByUserId = currentUser.Id;
        report.FinalizedAt = DateTime.UtcNow;
        report.ReviewComments = dto.Comments;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToReportDto(report));
    }

    [HttpPost("api/carry-over/reports/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ApproveRejectDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        if (!IsManagerOrAbove(currentUser))
            return Forbid();

        var report = await dbContext.CarryOverReports
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (report is null) return NotFound();

        if (report.Status != CarryOverReportStatus.Submitted && report.Status != CarryOverReportStatus.Reviewed)
            return BadRequest("Only submitted or reviewed reports can be rejected");

        report.Status = CarryOverReportStatus.Reviewed;
        report.ReviewedByUserId = currentUser.Id;
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewComments = dto.Comments;
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

    private static IQueryable<CarryOverReport> ApplyScope(IQueryable<CarryOverReport> query, ApplicationUser currentUser)
    {
        return currentUser.Role switch
        {
            UserRole.Director or UserRole.SystemAdmin => query,
            UserRole.Manager when currentUser.SectionId.HasValue => query.Where(x => x.SectionId == currentUser.SectionId.Value),
            UserRole.TeamLeader when currentUser.HangarId.HasValue => query.Where(x => x.HangarId == currentUser.HangarId.Value),
            UserRole.TeamLeader when currentUser.ShopId.HasValue => query.Where(x => x.AssignedToUserId == currentUser.Id),
            UserRole.Employee => query.Where(x => x.AssignedToUserId == currentUser.Id),
            _ => query.Where(_ => false)
        };
    }

    private static bool CanEditReport(CarryOverReport report, ApplicationUser currentUser)
    {
        return currentUser.Role switch
        {
            UserRole.Director or UserRole.SystemAdmin => true,
            UserRole.Manager when currentUser.SectionId == report.SectionId => true,
            UserRole.TeamLeader when currentUser.HangarId == report.HangarId => true,
            UserRole.Employee when report.AssignedToUserId == currentUser.Id => true,
            _ => false
        };
    }

    private static bool IsManagerOrAbove(ApplicationUser currentUser)
    {
        return currentUser.Role is UserRole.Manager or UserRole.Director or UserRole.SystemAdmin;
    }

    private static string GenerateReportNumber()
    {
        return $"CO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private static object ToReportDto(CarryOverReport report)
    {
        var tasks = report.Tasks.Select(task => new
        {
            id = task.Id,
            title = task.Title,
            description = task.Description,
            category = task.Category,
            priority = task.Priority,
            status = task.Status.ToString(),
            taskType = task.TaskType.ToString(),
            deferralReason = task.DeferralReason.ToString(),
            deferralDetails = task.DeferralDetails,
            deferredTaskOrigin = task.DeferredTaskOrigin.ToString(),
            assignedToUserId = task.AssignedToUserId,
            estimatedHours = task.EstimatedHours,
            actualHours = task.ActualHours,
            startDate = task.StartDate,
            dueDate = task.DueDate,
            completedDate = task.CompletedDate,
            notes = task.Notes,
            taskCardNumber = task.TaskCardNumber,
            partRequestId = task.PartRequestId
        }).ToArray();

        return new
        {
            id = report.Id,
            reportNumber = report.ReportNumber,
            title = report.Title,
            description = report.Description,
            aircraftRegistration = report.AircraftRegistration,
            aircraftType = report.AircraftType,
            projectType = report.ProjectType,
            priority = report.Priority,
            status = report.Status.ToString(),
            dueDate = report.DueDate,
            completedDate = report.CompletedDate,
            sectionId = report.SectionId,
            section = report.Section?.Name,
            hangarId = report.HangarId,
            hangar = report.Hangar?.Name,
            assignedToUserId = report.AssignedToUserId,
            assignedToUser = report.AssignedToUser != null ? new { id = report.AssignedToUser.Id, fullName = report.AssignedToUser.FullName, email = report.AssignedToUser.Email } : null,
            carryOverTasks = report.CarryOverTasks,
            carryOverPercentage = report.CarryOverPercentage,
            remarks = report.Remarks,
            submittedByUserId = report.SubmittedByUserId,
            submittedAt = report.SubmittedAt,
            submittedBy = report.SubmittedByUser != null ? new { id = report.SubmittedByUser.Id, fullName = report.SubmittedByUser.FullName } : null,
            reviewedByUserId = report.ReviewedByUserId,
            reviewedAt = report.ReviewedAt,
            reviewedBy = report.ReviewedByUser != null ? new { id = report.ReviewedByUser.Id, fullName = report.ReviewedByUser.FullName } : null,
            reviewComments = report.ReviewComments,
            finalizedByUserId = report.FinalizedByUserId,
            finalizedAt = report.FinalizedAt,
            finalizedBy = report.FinalizedByUser != null ? new { id = report.FinalizedByUser.Id, fullName = report.FinalizedByUser.FullName } : null,
            createdAt = report.CreatedAt,
            updatedAt = report.UpdatedAt,
            tasks
        };
    }
}

public record CreateCarryOverReportDto(
    string Title,
    string Description,
    string AircraftRegistration,
    string AircraftType,
    string ProjectType,
    string Priority,
    DateTime DueDate,
    Guid SectionId,
    Guid? HangarId,
    Guid? AssignedToUserId,
    List<CreateCarryOverTaskDto> Tasks
);

public record CreateCarryOverTaskDto(
    string Title,
    string Description,
    string Category,
    string Priority,
    CarryOverTaskType TaskType,
    CarryOverDeferralReason DeferralReason,
    string? DeferralDetails,
    CarryOverTaskOrigin DeferredTaskOrigin,
    Guid? AssignedToUserId,
    decimal EstimatedHours
);

public record UpdateCarryOverReportDto(
    string? Title,
    string? Description,
    string? Priority,
    DateTime? DueDate,
    Guid? AssignedToUserId,
    string? Remarks
);

public record ApproveRejectDto(string? Comments);
