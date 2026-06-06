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
public sealed class MaintenanceProjectController(BaseOpsDbContext dbContext) : ControllerBase
{
    [HttpGet("api/aums/projects")]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] Guid? sectionId = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var query = ApplyScope(dbContext.MaintenanceProjects
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .Include(x => x.ProjectManagerUser), currentUser);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MaintenanceProjectStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (sectionId.HasValue)
        {
            query = query.Where(x => x.SectionId == sectionId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var projects = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = projects.Select(ToProjectDto).ToArray();
        return Ok(ApiResults.Page<object>(items, total, pageNumber, pageSize));
    }

    [HttpGet("api/aums/projects/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var project = await ApplyScope(dbContext.MaintenanceProjects
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .Include(x => x.ProjectManagerUser)
            .Include(x => x.ProgressLogs)
            .Include(x => x.PartFollowUps), currentUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return project is null ? NotFound() : Ok(ToProjectDto(project));
    }

    [HttpPost("api/aums/projects")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceProjectDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var project = new MaintenanceProject
        {
            Id = Guid.NewGuid(),
            ProjectNumber = GenerateProjectNumber(),
            Title = dto.Title,
            Description = dto.Description,
            AircraftRegistration = dto.AircraftRegistration,
            AircraftType = dto.AircraftType,
            FleetType = dto.FleetType,
            Type = dto.Type,
            Status = MaintenanceProjectStatus.Draft,
            ScheduledStartDate = dto.ScheduledStartDate,
            ScheduledEndDate = dto.ScheduledEndDate,
            CompletionPercentage = 0,
            SectionId = dto.SectionId,
            HangarId = dto.HangarId,
            ShopId = dto.ShopId,
            ProjectManagerUserId = dto.ProjectManagerUserId,
            IsDelayed = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.Id
        };

        dbContext.MaintenanceProjects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, ToProjectDto(project));
    }

    [HttpPut("api/aums/projects/{id:guid}")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMaintenanceProjectDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var project = await ApplyScope(dbContext.MaintenanceProjects, currentUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (project is null) return NotFound();

        if (!CanEditProject(project, currentUser))
            return Forbid();

        if (dto.Title is not null) project.Title = dto.Title;
        if (dto.Description is not null) project.Description = dto.Description;
        if (dto.ScheduledStartDate is not null) project.ScheduledStartDate = dto.ScheduledStartDate.Value;
        if (dto.ScheduledEndDate is not null) project.ScheduledEndDate = dto.ScheduledEndDate.Value;
        if (dto.ActualStartDate is not null) project.ActualStartDate = dto.ActualStartDate;
        if (dto.ActualEndDate is not null) project.ActualEndDate = dto.ActualEndDate;
        if (dto.CompletionPercentage is not null) project.CompletionPercentage = dto.CompletionPercentage.Value;
        if (dto.ProjectManagerUserId is not null) project.ProjectManagerUserId = dto.ProjectManagerUserId;
        if (dto.Remarks is not null) project.Remarks = dto.Remarks;
        if (dto.IsDelayed is not null) project.IsDelayed = dto.IsDelayed.Value;
        if (dto.DelayReason is not null) project.DelayReason = dto.DelayReason;
        project.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToProjectDto(project));
    }

    [HttpGet("api/aums/projects/{projectId:guid}/progress-logs")]
    public async Task<IActionResult> GetProgressLogs(Guid projectId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var project = await ApplyScope(dbContext.MaintenanceProjects, currentUser)
            .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

        if (project is null) return NotFound();

        var logs = await dbContext.DailyProgressLogs
            .AsNoTracking()
            .Include(x => x.SubmittedByUser)
            .Where(x => x.MaintenanceProjectId == projectId)
            .OrderByDescending(x => x.LogDate)
            .ToListAsync(cancellationToken);

        return Ok(logs.Select(ToProgressLogDto).ToArray());
    }

    [HttpPost("api/aums/projects/{projectId:guid}/progress-logs")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> CreateProgressLog(Guid projectId, [FromBody] CreateProgressLogDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var project = await ApplyScope(dbContext.MaintenanceProjects, currentUser)
            .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

        if (project is null) return NotFound();

        if (!CanEditProject(project, currentUser))
            return Forbid();

        var log = new DailyProgressLog
        {
            Id = Guid.NewGuid(),
            MaintenanceProjectId = projectId,
            LogDate = dto.LogDate,
            WorkPerformed = dto.WorkPerformed,
            PlannedHours = dto.PlannedHours,
            ActualHours = dto.ActualHours,
            ManpowerCount = dto.ManpowerCount,
            IssuesEncountered = dto.IssuesEncountered,
            NextDayPlan = dto.NextDayPlan,
            IsSubmitted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.Id
        };

        dbContext.DailyProgressLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToProgressLogDto(log));
    }

    [HttpGet("api/aums/projects/{projectId:guid}/part-follow-ups")]
    public async Task<IActionResult> GetPartFollowUps(Guid projectId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var project = await ApplyScope(dbContext.MaintenanceProjects, currentUser)
            .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

        if (project is null) return NotFound();

        var parts = await dbContext.PartFollowUps
            .AsNoTracking()
            .Include(x => x.AssignedToUser)
            .Where(x => x.MaintenanceProjectId == projectId)
            .OrderBy(x => x.RequiredBy)
            .ToListAsync(cancellationToken);

        return Ok(parts.Select(ToPartFollowUpDto).ToArray());
    }

    [HttpPost("api/aums/projects/{projectId:guid}/part-follow-ups")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> CreatePartFollowUp(Guid projectId, [FromBody] CreatePartFollowUpDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var project = await ApplyScope(dbContext.MaintenanceProjects, currentUser)
            .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

        if (project is null) return NotFound();

        if (!CanEditProject(project, currentUser))
            return Forbid();

        var part = new PartFollowUp
        {
            Id = Guid.NewGuid(),
            MaintenanceProjectId = projectId,
            PartNumber = dto.PartNumber,
            PartName = dto.PartName,
            Description = dto.Description,
            Status = dto.Status,
            RequiredBy = dto.RequiredBy,
            Supplier = dto.Supplier,
            Cost = dto.Cost,
            Remarks = dto.Remarks,
            AssignedToUserId = dto.AssignedToUserId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.Id
        };

        dbContext.PartFollowUps.Add(part);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToPartFollowUpDto(part));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return null;
        return await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
    }

    private static IQueryable<MaintenanceProject> ApplyScope(IQueryable<MaintenanceProject> query, ApplicationUser currentUser)
    {
        return currentUser.Role switch
        {
            UserRole.Director or UserRole.SystemAdmin => query,
            UserRole.Manager when currentUser.SectionId.HasValue => query.Where(x => x.SectionId == currentUser.SectionId.Value),
            UserRole.TeamLeader when currentUser.HangarId.HasValue => query.Where(x => x.HangarId == currentUser.HangarId.Value),
            UserRole.TeamLeader when currentUser.ShopId.HasValue => query.Where(x => x.ShopId == currentUser.ShopId.Value),
            UserRole.Employee => query.Where(x => x.ProjectManagerUserId == currentUser.Id),
            _ => query.Where(_ => false)
        };
    }

    private static bool CanEditProject(MaintenanceProject project, ApplicationUser currentUser)
    {
        return currentUser.Role switch
        {
            UserRole.Director or UserRole.SystemAdmin => true,
            UserRole.Manager when currentUser.SectionId == project.SectionId => true,
            UserRole.TeamLeader when currentUser.HangarId == project.HangarId => true,
            UserRole.TeamLeader when currentUser.ShopId == project.ShopId => true,
            UserRole.Employee when project.ProjectManagerUserId == currentUser.Id => true,
            _ => false
        };
    }

    private static string GenerateProjectNumber()
    {
        return $"AUMS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private static object ToProjectDto(MaintenanceProject project)
    {
        var progressLogs = project.ProgressLogs.Select(ToProgressLogDto).ToArray();
        var partFollowUps = project.PartFollowUps.Select(ToPartFollowUpDto).ToArray();

        return new
        {
            id = project.Id,
            projectNumber = project.ProjectNumber,
            title = project.Title,
            description = project.Description,
            aircraftRegistration = project.AircraftRegistration,
            aircraftType = project.AircraftType,
            fleetType = project.FleetType,
            type = project.Type.ToString(),
            status = project.Status.ToString(),
            scheduledStartDate = project.ScheduledStartDate,
            actualStartDate = project.ActualStartDate,
            scheduledEndDate = project.ScheduledEndDate,
            actualEndDate = project.ActualEndDate,
            completionPercentage = project.CompletionPercentage,
            sectionId = project.SectionId,
            section = project.Section?.Name,
            hangarId = project.HangarId,
            hangar = project.Hangar?.Name,
            shopId = project.ShopId,
            shop = project.Shop?.Name,
            projectManagerUserId = project.ProjectManagerUserId,
            projectManager = project.ProjectManagerUser != null ? new { id = project.ProjectManagerUser.Id, fullName = project.ProjectManagerUser.FullName, email = project.ProjectManagerUser.Email } : null,
            remarks = project.Remarks,
            isDelayed = project.IsDelayed,
            delayReason = project.DelayReason,
            createdAt = project.CreatedAt,
            updatedAt = project.UpdatedAt,
            progressLogs,
            partFollowUps
        };
    }

    private static object ToProgressLogDto(DailyProgressLog log)
    {
        return new
        {
            id = log.Id,
            maintenanceProjectId = log.MaintenanceProjectId,
            logDate = log.LogDate,
            workPerformed = log.WorkPerformed,
            plannedHours = log.PlannedHours,
            actualHours = log.ActualHours,
            manpowerCount = log.ManpowerCount,
            issuesEncountered = log.IssuesEncountered,
            nextDayPlan = log.NextDayPlan,
            isSubmitted = log.IsSubmitted,
            submittedAt = log.SubmittedAt,
            submittedByUserId = log.SubmittedByUserId,
            submittedBy = log.SubmittedByUser != null ? new { id = log.SubmittedByUser.Id, fullName = log.SubmittedByUser.FullName } : null,
            createdAt = log.CreatedAt,
            updatedAt = log.UpdatedAt
        };
    }

    private static object ToPartFollowUpDto(PartFollowUp part)
    {
        return new
        {
            id = part.Id,
            maintenanceProjectId = part.MaintenanceProjectId,
            partNumber = part.PartNumber,
            partName = part.PartName,
            description = part.Description,
            status = part.Status,
            requiredBy = part.RequiredBy,
            orderedDate = part.OrderedDate,
            receivedDate = part.ReceivedDate,
            supplier = part.Supplier,
            cost = part.Cost,
            remarks = part.Remarks,
            assignedToUserId = part.AssignedToUserId,
            assignedTo = part.AssignedToUser != null ? new { id = part.AssignedToUser.Id, fullName = part.AssignedToUser.FullName, email = part.AssignedToUser.Email } : null,
            createdAt = part.CreatedAt,
            updatedAt = part.UpdatedAt
        };
    }
}

public record CreateMaintenanceProjectDto(
    string Title,
    string Description,
    string AircraftRegistration,
    string AircraftType,
    string FleetType,
    ProjectType Type,
    DateTime ScheduledStartDate,
    DateTime ScheduledEndDate,
    Guid SectionId,
    Guid? HangarId,
    Guid? ShopId,
    Guid? ProjectManagerUserId
);

public record UpdateMaintenanceProjectDto(
    string? Title,
    string? Description,
    DateTime? ScheduledStartDate,
    DateTime? ScheduledEndDate,
    DateTime? ActualStartDate,
    DateTime? ActualEndDate,
    int? CompletionPercentage,
    Guid? ProjectManagerUserId,
    string? Remarks,
    bool? IsDelayed,
    string? DelayReason
);

public record CreateProgressLogDto(
    DateTime LogDate,
    string WorkPerformed,
    int PlannedHours,
    int ActualHours,
    int ManpowerCount,
    string? IssuesEncountered,
    string? NextDayPlan
);

public record CreatePartFollowUpDto(
    string PartNumber,
    string PartName,
    string Description,
    string Status,
    DateTime RequiredBy,
    string? Supplier,
    decimal? Cost,
    string? Remarks,
    Guid? AssignedToUserId
);
