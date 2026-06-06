using BaseOps.API.Models;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
public sealed class DehangaringController(BaseOpsDbContext dbContext) : ControllerBase
{
    [HttpGet("api/dehangaring-reports/list")]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? section = null, [FromQuery] string? hangar = null, [FromQuery] string? date = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        // Log total inspections in database before any filtering
        var totalInDb = await dbContext.SafaInspections.CountAsync(cancellationToken);
        Console.WriteLine($"[DehangaringController] Total inspections in database: {totalInDb}");
        
        // Log inspection details for debugging
        var allInspections = await dbContext.SafaInspections
            .AsNoTracking()
            .Select(x => new { x.Id, x.Status, x.HangarId, x.ShopId, x.SectionId })
            .ToListAsync(cancellationToken);
        foreach (var insp in allInspections)
        {
            Console.WriteLine($"[DehangaringController] Inspection: {insp.Id}, Status: {insp.Status}, HangarId: {insp.HangarId}, ShopId: {insp.ShopId}, SectionId: {insp.SectionId}");
        }
        
        // Log defects directly from database
        var allDefects = await dbContext.SafaDefects.CountAsync(cancellationToken);
        Console.WriteLine($"[DehangaringController] Total defects in database: {allDefects}");
        var defectsByInspection = await dbContext.SafaDefects
            .GroupBy(d => d.InspectionId)
            .Select(g => new { InspectionId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        foreach (var group in defectsByInspection)
        {
            Console.WriteLine($"[DehangaringController] Inspection {group.InspectionId} has {group.Count} defects");
        }

        var query = ApplyScope(dbContext.SafaInspections
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .Include(x => x.Inspector)
            .Include(x => x.Defects), currentUser);

        // Include all inspections (Draft, In Progress, Submitted, Completed) to show all logged defects
        // query = query.Where(x => x.Status == InspectionStatus.Submitted || x.Status == InspectionStatus.Completed);

        if (!string.IsNullOrWhiteSpace(section))
        {
            query = query.Where(x => x.Section != null && x.Section.Name == section);
        }

        if (!string.IsNullOrWhiteSpace(hangar))
        {
            query = query.Where(x => x.Hangar != null && x.Hangar.Name == hangar);
        }

        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
        {
            query = query.Where(x => x.InspectionDate.Date == parsedDate.Date);
        }

        var total = await query.CountAsync(cancellationToken);
        var inspections = await query
            .OrderByDescending(x => x.InspectionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Debug logging
        Console.WriteLine($"[DehangaringController] Total inspections: {total}");
        Console.WriteLine($"[DehangaringController] Inspections returned: {inspections.Count}");
        foreach (var inspection in inspections)
        {
            Console.WriteLine($"[DehangaringController] Inspection: {inspection.Id}, Status: {inspection.Status}, Defects: {inspection.Defects.Count}");
        }

        var items = inspections.Select(ToReportDto).ToArray();
        return Ok(ApiResults.Page<object>(items, total, pageNumber, pageSize));
    }

    [HttpGet("api/dehangaring-reports/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var inspection = await ApplyScope(dbContext.SafaInspections
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .Include(x => x.Inspector)
            .Include(x => x.Defects), currentUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return inspection is null ? NotFound() : Ok(ToReportDto(inspection));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return null;
        return await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
    }

    private static IQueryable<SafaInspection> ApplyScope(IQueryable<SafaInspection> query, ApplicationUser currentUser)
    {
        Console.WriteLine($"[DehangaringController] ApplyScope - User Role: {currentUser.Role}, SectionId: {currentUser.SectionId}, HangarId: {currentUser.HangarId}, ShopId: {currentUser.ShopId}");
        
        var result = currentUser.Role switch
        {
            UserRole.Director or UserRole.SystemAdmin or UserRole.SafetyInspector => query,
            // SafaInspector can see all inspections, but also ensure they can see their own created inspections
            UserRole.SafaInspector => query,
            UserRole.Manager when currentUser.SectionId.HasValue => query.Where(x => x.SectionId == currentUser.SectionId.Value || x.InspectorId == currentUser.Id),
            // Allow TeamLeader to see inspections in their section OR their specific hangar/shop OR their own created inspections
            UserRole.TeamLeader when currentUser.SectionId.HasValue => query.Where(x => x.SectionId == currentUser.SectionId.Value || x.InspectorId == currentUser.Id),
            UserRole.TeamLeader when currentUser.HangarId.HasValue => query.Where(x => x.HangarId == currentUser.HangarId.Value || x.InspectorId == currentUser.Id),
            UserRole.TeamLeader when currentUser.ShopId.HasValue => query.Where(x => x.ShopId == currentUser.ShopId.Value || x.InspectorId == currentUser.Id),
            UserRole.Employee => query.Where(x => x.InspectorId == currentUser.Id),
            _ => query.Where(_ => false)
        };
        
        Console.WriteLine($"[DehangaringController] ApplyScope - Filter applied");
        return result;
    }

    private static object ToReportDto(SafaInspection inspection)
    {
        var defects = inspection.Defects.Select(defect => new
        {
            id = defect.Id,
            description = defect.ObservationFinding,
            category = defect.Category,
            classification = defect.NeedToFix ? "Major" : "Minor",
            status = MapDefectStatus(defect.Status),
            resolvedAt = defect.ActionTakenAt,
            correctiveAction = defect.CorrectiveAction,
            assignedTo = defect.ActionTakenByUserId
        }).ToArray();

        return new
        {
            id = inspection.Id,
            createdAt = inspection.CreatedAt,
            updatedAt = inspection.UpdatedAt ?? inspection.CreatedAt,
            date = inspection.InspectionDate,
            section = inspection.Section?.Name ?? string.Empty,
            aircraftTailNumber = inspection.AircraftRegistration,
            aircraftType = inspection.FleetType,
            hangar = inspection.Hangar?.Name ?? inspection.Shop?.Name ?? string.Empty,
            inspector = new
            {
                id = inspection.Inspector.Id,
                employeeId = inspection.Inspector.EmployeeId,
                fullName = inspection.Inspector.FullName,
                email = inspection.Inspector.Email,
                role = inspection.Inspector.Role.ToString(),
                isActive = inspection.Inspector.IsActive,
                mustChangePassword = inspection.Inspector.MustChangePassword
            },
            teamLeader = (object?)null,
            status = inspection.Status == InspectionStatus.Completed ? "Cleared" : "PendingInspection",
            workPackageRef = inspection.FlightInfo ?? string.Empty,
            defects,
            completionPercentage = defects.Length == 0 ? 100 : defects.Count(x => x.status == "Cleared") * 100 / defects.Length,
            remarks = inspection.Conclusion,
            approvedAt = inspection.SubmittedAt,
            approvedBy = (object?)null
        };
    }

    private static string MapDefectStatus(DefectStatus status) => status switch
    {
        DefectStatus.Cleared => "Cleared",
        DefectStatus.WaitingForPart => "Waiting for Part",
        _ => "Active"
    };
}
