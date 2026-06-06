using BaseOps.API.Models;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaseOps.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class IntegrationCoreController(BaseOpsDbContext dbContext) : ControllerBase
{
    // Employee profile endpoints moved to dedicated EmployeeProfileController
    // These routes are kept for backward compatibility and will be deprecated

    [HttpGet("audit")]
    [HttpGet("audit/logs")]
    public async Task<ActionResult<PaginatedResult<object>>> Audit([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var query = dbContext.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { id = x.Id, userId = x.UserId, action = x.Action, module = x.EntityName, entityType = x.EntityName, entityId = x.EntityId, timestamp = x.CreatedAt, ipAddress = x.IpAddress, correlationId = x.CorrelationId, oldValues = x.BeforeValues, newValues = x.AfterValues }).ToArrayAsync(ct);
        return Ok(ApiResults.Page<object>(items, total, page, pageSize));
    }

    [HttpGet("audit/suspicious")]
    public IActionResult SuspiciousAudit() => Ok(Array.Empty<object>());

    [HttpGet("dashboard/stats")]
    [HttpGet("management/dashboard")]
    public async Task<IActionResult> DashboardStats(CancellationToken ct)
    {
        var users = await dbContext.Users.CountAsync(ct);
        var sections = await dbContext.Sections.CountAsync(ct);
        var hangars = await dbContext.Hangars.CountAsync(ct);
        return Ok(new { totalUsers = users, activeUsers = users, totalSections = sections, totalHangars = hangars, totalAircraft = 0, activeProjects = 0, pendingApprovals = 0, openTasks = 0, overdueItems = 0 });
    }

    [HttpGet("dashboard/super-admin")]
    public async Task<IActionResult> SuperAdminDashboard(CancellationToken ct)
    {
        var users = await dbContext.Users.CountAsync(ct);
        var sections = await dbContext.Sections.CountAsync(ct);
        var hangars = await dbContext.Hangars.CountAsync(ct);
        
        // Get real maintenance project data
        var maintenanceProjects = await dbContext.MaintenanceProjects
            .AsNoTracking()
            .ToListAsync(ct);
        
        var activeProjects = maintenanceProjects.Count(p => p.Status == MaintenanceProjectStatus.InProgress);
        var completedWorkPackages = maintenanceProjects.Count(p => p.Status == MaintenanceProjectStatus.Completed);
        var delayedAircraft = maintenanceProjects.Count(p => p.IsDelayed);
        var overdueWorkPackages = maintenanceProjects.Count(p => p.ScheduledEndDate < DateTime.UtcNow && p.Status != MaintenanceProjectStatus.Completed);
        
        // Get carry-over reports
        var carryOverReports = await dbContext.CarryOverReports
            .AsNoTracking()
            .ToListAsync(ct);
        
        // Build section performance data from real metrics
        var sectionPerformance = await dbContext.Sections
            .AsNoTracking()
            .Select(s => new
            {
                section = s.Name,
                efficiency = 0.0, // Placeholder - calculate from real metrics
                quality = 0.0, // Placeholder - calculate from real metrics
                score = 0.0 // Placeholder - calculate from real metrics
            })
            .ToArrayAsync(ct);

        return Ok(new
        {
            totalUsers = users,
            activeUsers = users,
            totalSections = sections,
            totalHangars = hangars,
            totalAircraft = 0,
            activeProjects = activeProjects,
            pendingApprovals = 0,
            openTasks = 0,
            overdueItems = overdueWorkPackages,
            totalAircraftUnderMaintenance = activeProjects,
            delayedAircraft = delayedAircraft,
            completedWorkPackages = completedWorkPackages,
            overdueWorkPackages = overdueWorkPackages,
            progressTrends = Array.Empty<object>(),
            fleetWorkloadDistribution = Array.Empty<object>(),
            enterpriseSLACompliance = 0,
            sectionPerformance = sectionPerformance,
            crossSectionMetrics = new
            {
                totalSections = sections,
                sectionsWithDelays = delayedAircraft,
                bestPerforming = "",
                needsAttention = "",
                averageEfficiency = 0.0,
                qualityCompliance = 0.0
            },
            resourceUtilization = Array.Empty<object>(),
            approvalThroughput = new
            {
                approvedToday = 0,
                rejectedToday = 0,
                averageApprovalTime = 0,
                pendingApprovals = 0,
                criticalDelays = 0
            },
            enterpriseRisks = Array.Empty<object>(),
            carryOverReports = carryOverReports.Select(r => new {
                r.Id,
                r.ReportNumber,
                r.Status,
                r.CreatedAt,
                r.AircraftRegistration,
                r.Section,
                r.CarryOverPercentage
            }).ToArray()
        });
    }

    [HttpGet("dashboard/section-overview")]
    [HttpGet("v1/dashboard/summary")]
    [HttpGet("dashboard/sections")]
    public async Task<IActionResult> SectionOverview(CancellationToken ct)
    {
        var items = await dbContext.Sections.AsNoTracking().Select(x => new { sectionId = x.Id, sectionName = x.Name, name = x.Name, employeeCount = dbContext.Users.Count(u => u.SectionId == x.Id), activeProjects = 0, completionRate = 0 }).ToArrayAsync(ct);
        return Ok(items);
    }

    [HttpGet("dashboard/team-leader")]
    public async Task<IActionResult> TeamLeaderDashboard([FromQuery] Guid? sectionId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return Unauthorized();

        // Apply RBAC filtering based on user's role (same logic as ScheduleController)
        var query = dbContext.Users
            .AsNoTracking()
            .Include(u => u.Section)
            .Where(u => u.IsActive);

        if (user.Role == UserRole.Employee)
        {
            // Employees see only employees in their same hangar (same team)
            if (user.HangarId.HasValue)
                query = query.Where(x => x.HangarId == user.HangarId);
            else if (user.ShopId.HasValue)
                query = query.Where(x => x.ShopId == user.ShopId);
        }
        else if (user.Role == UserRole.TeamLeader)
        {
            // Team leaders see only employees that report to them (via ReportsToUserId)
            query = query.Where(x => x.ReportsToUserId == userId && x.Role == UserRole.Employee);
        }
        else if (user.Role == UserRole.Manager)
        {
            // Managers see employees in their section
            if (user.SectionId.HasValue)
                query = query.Where(x => x.SectionId == user.SectionId);
        }
        // Directors and Admins see all employees (no filter)

        var teamMembers = await query
            .Select(u => new
            {
                id = u.Id,
                fullName = u.FullName,
                employeeId = u.EmployeeId,
                role = u.Role.ToString(),
                section = u.Section != null ? u.Section.Name : string.Empty,
            })
            .ToArrayAsync(ct);

        var dehangaringStats = new[]
        {
            new { label = "Active", count = 0, cls = "bg-blue-500", text = "text-blue-600" },
            new { label = "Cleared", count = 0, cls = "bg-emerald-500", text = "text-status-approved" },
            new { label = "Waiting", count = 0, cls = "bg-amber-500", text = "text-status-warning" },
        };

        return Ok(new
        {
            teamMembers,
            dehangaringStats,
        });
    }

    [HttpGet("dashboard/manager/{sectionId}")]
    public async Task<IActionResult> ManagerDashboard(Guid sectionId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return Unauthorized();

        // Managers can only access their own section's dashboard
        if (user.Role == UserRole.Manager && user.SectionId != sectionId)
            return Forbid();

        // Get team members in the section
        var teamMembers = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Section)
            .Where(u => u.SectionId == sectionId && u.IsActive)
            .Select(u => new
            {
                id = u.Id,
                fullName = u.FullName,
                employeeId = u.EmployeeId,
                role = u.Role.ToString(),
                section = u.Section != null ? u.Section.Name : string.Empty,
            })
            .ToArrayAsync(ct);

        // Return empty arrays for modules not yet implemented
        var carryOverReports = Array.Empty<object>();
        var dehangaringReports = new
        {
            cabin = new { totalDefects = 0, activeDefects = 0, clearedDefects = 0, waitingForPartDefects = 0, defectClosureRate = 0.0 },
            amt = new { totalDefects = 0, activeDefects = 0, clearedDefects = 0, waitingForPartDefects = 0, defectClosureRate = 0.0 }
        };

        return Ok(new
        {
            teamMembers,
            carryOverReports,
            dehangaringReports,
        });
    }

    [HttpGet("v1/dashboard/metrics")]
    public async Task<IActionResult> DashboardMetrics([FromQuery] Guid? sectionId, CancellationToken ct)
    {
        var usersQuery = dbContext.Users.AsNoTracking();
        var sectionsQuery = dbContext.Sections.AsNoTracking();

        if (sectionId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.SectionId == sectionId.Value);
            sectionsQuery = sectionsQuery.Where(s => s.Id == sectionId.Value);
        }

        var users = await usersQuery.CountAsync(ct);
        var sections = await sectionsQuery.CountAsync(ct);
        var hangars = sectionId.HasValue 
            ? await dbContext.Hangars.Where(h => h.SectionId == sectionId.Value).CountAsync(ct)
            : await dbContext.Hangars.CountAsync(ct);
        var shops = sectionId.HasValue
            ? await dbContext.Shops.Where(s => s.SectionId == sectionId.Value).CountAsync(ct)
            : await dbContext.Shops.CountAsync(ct);

        return Ok(new { 
            totalUsers = users, 
            activeUsers = users, 
            totalSections = sections, 
            totalHangars = hangars,
            totalShops = shops,
            totalAircraft = 0, 
            activeProjects = 0, 
            pendingApprovals = 0, 
            openTasks = 0, 
            overdueItems = 0 
        });
    }

    [HttpGet("management")]
    public IActionResult ManagementInfo() => Ok(new { name = "BaseOps Management", stats = new { } });

    private static object ProfileDto(BaseOps.Domain.Entities.ApplicationUser x)
    {
        var parts = x.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return new { 
            id = x.Id, 
            firstName = parts.FirstOrDefault() ?? x.FullName, 
            lastName = parts.Length > 1 ? parts[1] : string.Empty, 
            employeeId = x.EmployeeId, 
            fullName = x.FullName, 
            email = x.Email ?? string.Empty, 
            phoneNumber = x.PhoneNumber ?? string.Empty, 
            address = x.Address ?? string.Empty, 
            position = x.Role.ToString(), 
            emergencyContactName = x.EmergencyContactName ?? string.Empty,
            emergencyContactPhoneNumber = x.EmergencyContactPhoneNumber ?? string.Empty,
            maintenanceAuthorizationType = x.CompanyAuthorizationType?.ToString() ?? string.Empty, 
            role = x.Role.ToString(), 
            roleName = x.Role.ToString(), 
            sectionId = x.SectionId, 
            sectionName = x.Section?.Name, 
            hangarId = x.HangarId, 
            hangarName = x.Hangar?.Name, 
            shopId = x.ShopId, 
            shopName = x.Shop?.Name, 
            isActive = x.IsActive, 
            hireDate = x.CreatedAt,
            profilePhotoUrl = x.ProfilePhotoUrl ?? string.Empty
        };
    }
}
