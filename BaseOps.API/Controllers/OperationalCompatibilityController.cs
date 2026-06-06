using BaseOps.API.Models;
using BaseOps.API.Services;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BaseOps.API.Controllers;

[ApiController]
[Route("api/compatibility")]
[Authorize]
public sealed class OperationalCompatibilityController(BaseOpsDbContext dbContext) : ControllerBase
{
    [HttpGet("aums/dashboard")]
    [HttpGet("aums/analytics")]
    [HttpGet("dashboard/production")]
    public IActionResult AumsDashboard() => Ok(new { totalProjects = 0, onSchedule = 0, delayed = 0, critical = 0, completed = 0, bySection = Array.Empty<object>(), byStatus = Array.Empty<object>() });

    [HttpGet("aums/list")]
    [HttpGet("carryover/reports")]
    [HttpGet("carry-over/reports")]
    [HttpGet("post-mortem-reports/list")]
    [HttpGet("post-mortem/reports")]
    [HttpGet("material-order-statuses/list")]
    [HttpGet("mo/orders")]
    [HttpGet("safa/inspections")]
    [HttpGet("safa-inspections")]
    [HttpGet("annualleave/list")]
    [HttpGet("leave/requests")]
    [HttpGet("dailyassignment/list")]
    [HttpGet("handover-logbooks/list")]
    [HttpGet("monthly-schedules/list")]
    [HttpGet("dehangaring-reports/list")]
    [HttpGet("dehangaring-safa-defect-logs/list")]
    [HttpGet("reports/history")]
    public async Task<IActionResult> EmptyPage([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();
        var (module, resource) = ResolveModuleResource();
        var query = dbContext.OperationalRecords.AsNoTracking().Where(x => x.Module == module);
        if (!string.IsNullOrWhiteSpace(resource)) query = query.Where(x => x.Resource == resource);
        query = ApplyRecordScope(query, currentUser);
        var total = await query.CountAsync(cancellationToken);
        var records = await query.OrderByDescending(x => x.CreatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        var items = records.Select(ToDto).ToArray();
        return Ok(ApiResults.Page<object>(items, total, pageNumber, pageSize));
    }

    [HttpGet("aums/projects")]
    public async Task<IActionResult> AumsProjects(CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();
        var records = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "aums" || x.Module == "production")
            .Where(x => currentUser.Role == UserRole.Director || currentUser.Role == UserRole.SystemAdmin || x.CreatedBy == currentUser.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        var items = records.Select(ToDto).ToArray();
        return Ok(items);
    }

    [HttpGet("daily-status-reports/{id:guid}")]
    public async Task<IActionResult> DailyStatusReportById(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();
        var record = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "daily-status-reports" && x.Id == id)
            .Where(x => currentUser.Role == UserRole.Director || currentUser.Role == UserRole.SystemAdmin || x.CreatedBy == currentUser.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return record is null ? NotFound() : Ok(ToDto(record));
    }

    [HttpGet("post-mortem-reports/dashboard")]
    [HttpGet("post-mortem-reports/analytics")]
    [HttpGet("post-mortem/dashboard")]
    [HttpGet("carry-over/summary")]
    public IActionResult EmptyDashboard() => Ok(new { total = 0, totalReports = 0, totalOrders = 0, totalActivities = 0, totalInspections = 0, byStatus = Array.Empty<object>(), bySection = Array.Empty<object>(), byPriority = Array.Empty<object>(), monthlyTrend = Array.Empty<object>() });

    [HttpGet("safa/templates")]
    [HttpGet("safa-templates/aircraft-cabin")]
    [HttpGet("safa-templates/amt")]
    [HttpGet("dehangaring-safa-defect-logs/pending")]
    [HttpGet("dashboard/charts")]
    [HttpGet("post-mortem-reports/charts")]
    public IActionResult EmptyArray() => Ok(Array.Empty<object>());

    [HttpGet("annualleave/balance/{employeeId}")]
    [HttpGet("leave/balance")]
    public async Task<IActionResult> LeaveBalance(string? employeeId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var targetUserId = string.IsNullOrEmpty(employeeId) ? userId.Value : Guid.Parse(employeeId);
        if (targetUserId != userId.Value && currentUser.Role is not (UserRole.Manager or UserRole.Director or UserRole.SystemAdmin))
        {
            return Forbid();
        }

        // Get user info
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
        if (user is null) return NotFound();

        // Query leave records from operational_records
        var currentYear = DateTime.UtcNow.Year;
        var leaveRecords = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(r => r.Module == "annualleave" || r.Module == "leave")
            .Where(r => r.CreatedAt.Year == currentYear)
            .Where(r => r.PayloadJson != null)
            .ToListAsync(cancellationToken);

        int taken = 0;
        int pending = 0;

        // Parse records to calculate taken and pending days
        foreach (var record in leaveRecords)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
                if (payload.TryGetProperty("employeeId", out var empId) && 
                    empId.GetString() == targetUserId.ToString())
                {
                    if (payload.TryGetProperty("days", out var daysProp))
                    {
                        var days = daysProp.GetInt32();
                        if (record.Status == "Approved" || record.Status == "Completed")
                        {
                            taken += days;
                        }
                        else if (record.Status == "Pending" || record.Status == "Submitted")
                        {
                            pending += days;
                        }
                    }
                }
            }
            catch
            {
                // Skip malformed records
                continue;
            }
        }

        // Standard entitlement: 30 days per year (configurable per policy)
        var totalEntitled = 30;
        var remaining = totalEntitled - taken - pending;

        return Ok(new 
        { 
            employeeId = targetUserId.ToString(), 
            employeeName = user.FullName, 
            totalEntitled = totalEntitled, 
            taken = taken, 
            pending = pending, 
            remaining = remaining, 
            year = currentYear 
        });
    }

    [HttpGet("annualleave/plan")]
    public IActionResult PlanningSummary() => Ok(new { id = Guid.NewGuid(), assignments = Array.Empty<object>(), conflicts = Array.Empty<object>(), monthlySummary = Array.Empty<object>(), criticalPeriods = Array.Empty<object>(), recommendations = Array.Empty<string>() });

    [HttpGet("annualleave/manpower-summary")]
    public async Task<IActionResult> ManpowerSummary([FromQuery] string? sectionId, [FromQuery] string? startDate, [FromQuery] string? endDate, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();
        var usersQuery = dbContext.Users.AsNoTracking();
        if (currentUser.Role == UserRole.Manager && currentUser.SectionId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.SectionId == currentUser.SectionId.Value);
        }
        else if (currentUser.Role is UserRole.Employee or UserRole.TeamLeader)
        {
            usersQuery = usersQuery.Where(u => u.Id == currentUser.Id);
        }
        
        if (!string.IsNullOrEmpty(sectionId))
        {
            usersQuery = usersQuery.Where(u => u.SectionId == Guid.Parse(sectionId));
        }

        var users = await usersQuery.ToListAsync(cancellationToken);
        
        // Get leave records for the period
        var start = !string.IsNullOrEmpty(startDate) ? DateTime.Parse(startDate) : DateTime.UtcNow.AddDays(-30);
        var end = !string.IsNullOrEmpty(endDate) ? DateTime.Parse(endDate) : DateTime.UtcNow;
        
        var leaveRecords = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(r => (r.Module == "annualleave" || r.Module == "leave") && r.CreatedAt >= start && r.CreatedAt <= end)
            .Where(r => r.PayloadJson != null)
            .ToListAsync(cancellationToken);

        // Parse leave requests
        var leaveRequests = new List<CompatibilityLeaveRequest>();
        foreach (var record in leaveRecords)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
                if (payload.TryGetProperty("employeeId", out var empId) && 
                    users.Any(e => e.Id.ToString() == empId.GetString()))
                {
                    leaveRequests.Add(new CompatibilityLeaveRequest
                    {
                        EmployeeId = users.FirstOrDefault(u => u.Id.ToString() == empId.GetString())?.EmployeeId ?? empId.GetString() ?? string.Empty,
                        StartDate = payload.TryGetProperty("startDate", out var sd) ? sd.GetDateTime() : (DateTime?)null,
                        EndDate = payload.TryGetProperty("endDate", out var ed) ? ed.GetDateTime() : (DateTime?)null,
                        Days = payload.TryGetProperty("days", out var days) ? days.GetInt32() : 0,
                        Status = payload.TryGetProperty("status", out var status) ? status.GetString() ?? "Pending" : "Pending"
                    });
                }
            }
            catch
            {
                continue;
            }
        }

        // Calculate daily manpower
        var daysInRange = (int)(end - start).TotalDays;
        var dailyManpower = new List<object>();
        
        for (int i = 0; i <= daysInRange; i++)
        {
            var currentDate = start.AddDays(i);
            var onLeave = leaveRequests
                .Where(lr => lr.StartDate.HasValue && lr.EndDate.HasValue && 
                           currentDate >= lr.StartDate.Value && currentDate <= lr.EndDate.Value &&
                           (lr.Status == "Approved" || lr.Status == "Completed"))
                .ToList();

            var available = users.Count - onLeave.Count;
            
            dailyManpower.Add(new
            {
                date = currentDate.ToString("yyyy-MM-dd"),
                total = users.Count,
                available = available,
                onLeave = onLeave.Count,
                leavePercentage = users.Count > 0 ? (double)onLeave.Count / users.Count * 100 : 0
            });
        }

        var summary = new
        {
            sectionId = sectionId,
            startDate = start.ToString("yyyy-MM-dd"),
            endDate = end.ToString("yyyy-MM-dd"),
            totalEmployees = users.Count,
            averageDailyAvailability = dailyManpower.Any() ? dailyManpower.Average(d => (double)((dynamic)d).available) : 0,
            peakLeaveDate = dailyManpower.OrderByDescending(d => (double)((dynamic)d).onLeave).FirstOrDefault(),
            dailyManpower = dailyManpower,
            leaveRequests = leaveRequests.Count,
            approvedLeave = leaveRequests.Count(lr => lr.Status == "Approved" || lr.Status == "Completed"),
            pendingLeave = leaveRequests.Count(lr => lr.Status == "Pending" || lr.Status == "Submitted")
        };

        return Ok(summary);
    }

    [HttpGet("post-mortem-reports/export")]
    [HttpGet("post-mortem/export")]
    public async Task<IActionResult> PostMortemExport([FromQuery] string format = "json", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();
        var query = dbContext.OperationalRecords.AsNoTracking().Where(r => r.Module == "post-mortem" || r.Module == "postmortem");
        query = ApplyRecordScope(query, currentUser);

        if (startDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= endDate.Value);
        }

        var records = await query.OrderByDescending(r => r.CreatedAt).Take(1000).ToListAsync(cancellationToken);

        var exportData = records.Select(r => new
        {
            id = r.Id,
            module = r.Module,
            resource = r.Resource,
            action = r.Action,
            status = r.Status,
            payload = JsonSerializer.Deserialize<object>(r.PayloadJson),
            createdAt = r.CreatedAt,
            updatedAt = r.UpdatedAt ?? r.CreatedAt
        }).ToList();

        if (format.ToLower() == "json")
        {
            return Ok(new
            {
                exportDate = DateTime.UtcNow,
                recordCount = exportData.Count,
                dateRange = new { start = startDate, end = endDate },
                data = exportData
            });
        }

        // For PDF/Excel, return a placeholder response
        // In a full implementation, this would generate actual PDF/Excel files
        return Ok(new
        {
            message = "PDF and Excel export not yet implemented. Use format=json for data export.",
            exportDate = DateTime.UtcNow,
            recordCount = exportData.Count,
            data = exportData
        });
    }

    [HttpGet("daily-status-reports")]
    public async Task<IActionResult> DailyStatusReportsList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();
        var query = dbContext.OperationalRecords.AsNoTracking().Where(x => x.Module == "daily-status-reports");
        query = ApplyRecordScope(query, currentUser);
        var total = await query.CountAsync(cancellationToken);
        var records = await query.OrderByDescending(x => x.CreatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        var items = records.Select(ToDto).ToArray();
        return Ok(ApiResults.Page<object>(items, total, pageNumber, pageSize));
    }

    [HttpGet("daily-status-reports/dashboard")]
    public IActionResult DailyStatusReportsDashboard() => Ok(new { totalReports = 0, draftReports = 0, submittedReports = 0, approvedReports = 0, pendingReview = 0, totalTasks = 0, completedTasks = 0, inWorkTasks = 0, totalPartIssues = 0, criticalParts = 0, totalFindings = 0, openFindings = 0, criticalFindings = 0 });

    [HttpGet("daily-status-reports/{id:guid}/analytics")]
    public IActionResult DailyStatusReportAnalytics(Guid id) => Ok(new { taskStatusDistribution = new Dictionary<string, int>(), phaseDistribution = new Dictionary<string, int>(), completionPercentage = 0, partIssueDistribution = new Dictionary<string, int>(), findingSeverityDistribution = new Dictionary<string, int>(), findingStatusDistribution = new Dictionary<string, int>() });

    [HttpGet("daily-status-reports/{id:guid}/phase-progress")]
    public IActionResult DailyStatusReportPhaseProgress(Guid id) => Ok(Array.Empty<object>());

    [HttpGet("daily-status-reports/{id:guid}/overall-status")]
    public IActionResult DailyStatusReportOverallStatus(Guid id) => Ok(Array.Empty<object>());

    [HttpPost("{module}")]
    [HttpPost("{module}/{resource}")]
    [HttpPost("{module}/{id:guid}/{action}")]
    [HttpPut("{module}/{id:guid}")]
    [HttpPatch("{module}/{id:guid}/{action}")]
    [HttpDelete("{module}/{id:guid}")]
    public IActionResult AcceptedMutation([FromRoute] string module, string? resource = null, Guid? id = null, string? action = null, [FromBody] JsonElement? payload = null, CancellationToken cancellationToken = default)
    {
        return StatusCode(StatusCodes.Status410Gone, new { message = "Generic compatibility mutations are disabled. Use module-specific endpoints with validation, RBAC, audit logging, and workflow enforcement." });
    }

    private (string Module, string? Resource) ResolveModuleResource()
    {
        var segments = Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var module = segments.Length > 1 ? segments[1] : "unknown";
        var resource = segments.Length > 2 ? segments[2] : null;
        return (module, resource);
    }

    private static object ToDto(OperationalRecord x) => new
    {
        id = x.Id,
        module = x.Module,
        resource = x.Resource,
        action = x.Action,
        payload = JsonSerializer.Deserialize<object>(x.PayloadJson),
        status = x.Status,
        createdAt = x.CreatedAt,
        updatedAt = x.UpdatedAt ?? x.CreatedAt
    };

    [HttpGet("carryover/dashboard")]
    [HttpGet("carry-over/dashboard")]
    public async Task<IActionResult> CarryOverDashboard([FromQuery] string? sectionId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();
        var query = dbContext.OperationalRecords.AsNoTracking().Where(r => r.Module == "carryover" || r.Module == "carry-over");
        query = ApplyRecordScope(query, currentUser);
        
        if (!string.IsNullOrEmpty(sectionId))
        {
            query = query.Where(r => r.Resource == sectionId);
        }

        var records = await query.ToListAsync(cancellationToken);
        
        var dashboard = new
        {
            totalReports = records.Count,
            submittedReports = records.Count(r => r.Status == "Submitted" || r.Status == "Draft"),
            pendingReports = records.Count(r => r.Status == "Pending" || r.Status == "Under Review"),
            completedReports = records.Count(r => r.Status == "Completed"),
            byStatus = records
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToList(),
            weeklyTrends = records
                .GroupBy(r => new { year = r.CreatedAt.Year, week = System.Globalization.ISOWeek.GetWeekOfYear(r.CreatedAt.DateTime) })
                .Select(g => new
                {
                    year = g.Key.year,
                    week = g.Key.week,
                    count = g.Count()
                })
                .OrderBy(x => x.year).ThenBy(x => x.week)
                .ToList(),
            recentReports = records
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new
                {
                    id = r.Id,
                    reportNumber = ExtractReportNumber(r.PayloadJson),
                    status = r.Status,
                    createdAt = r.CreatedAt,
                    section = r.Resource
                })
                .ToList()
        };

        return Ok(dashboard);
    }

    [HttpGet("carryover/analytics")]
    [HttpGet("carry-over/analytics")]
    public async Task<IActionResult> CarryOverAnalytics([FromQuery] string? sectionId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();
        var query = dbContext.OperationalRecords.AsNoTracking().Where(r => r.Module == "carryover" || r.Module == "carry-over");
        query = ApplyRecordScope(query, currentUser);
        
        if (!string.IsNullOrEmpty(sectionId))
        {
            query = query.Where(r => r.Resource == sectionId);
        }
        if (startDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= endDate.Value);
        }

        var records = await query.ToListAsync(cancellationToken);
        
        var analytics = new
        {
            byStatus = records
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToList(),
            
            bySection = records
                .Where(r => !string.IsNullOrEmpty(r.Resource))
                .GroupBy(r => r.Resource)
                .Select(g => new { section = g.Key, count = g.Count() })
                .ToList(),
            
            byOrigin = records
                .Where(r => r.PayloadJson != null)
                .SelectMany(r => ExtractTaskOrigins(r.PayloadJson))
                .Where(o => !string.IsNullOrEmpty(o))
                .GroupBy(o => o)
                .Select(g => new { origin = g.Key, count = g.Count() })
                .ToList(),
            
            monthlyTrends = records
                .GroupBy(r => new { year = r.CreatedAt.Year, month = r.CreatedAt.Month })
                .Select(g => new
                {
                    year = g.Key.year,
                    month = g.Key.month,
                    count = g.Count(),
                    completed = g.Count(r => r.Status == "Completed")
                })
                .OrderBy(x => x.year).ThenBy(x => x.month)
                .ToList()
        };

        return Ok(analytics);
    }

    private static string? ExtractReportNumber(string? payloadJson)
    {
        if (string.IsNullOrEmpty(payloadJson)) return null;
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("reportNumber", out var reportNumber))
                return reportNumber.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string[] ExtractTaskOrigins(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("tasks", out var tasks) && tasks.ValueKind == JsonValueKind.Array)
            {
                var origins = new List<string>();
                foreach (var task in tasks.EnumerateArray())
                {
                    if (task.TryGetProperty("origin", out var origin))
                    {
                        origins.Add(origin.GetString() ?? "Unknown");
                    }
                }
                return origins.ToArray();
            }
            return Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return null;
        return await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
    }

    private static IQueryable<OperationalRecord> ApplyRecordScope(IQueryable<OperationalRecord> query, ApplicationUser currentUser)
    {
        return currentUser.Role switch
        {
            UserRole.Director or UserRole.SystemAdmin => query,
            _ => query.Where(x => x.CreatedBy == currentUser.Id)
        };
    }

    private class CompatibilityLeaveRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Days { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
