using BaseOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
public sealed class AumsController(BaseOpsDbContext dbContext) : ControllerBase
{
    [HttpGet("api/v1/aums/charts/progress-trends")]
    public async Task<IActionResult> ProgressTrends([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.OperationalRecords.AsNoTracking().Where(r => r.Module == "aums");
        
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);

        var records = await query.ToListAsync(cancellationToken);
        
        // Group by date and calculate average progress
        var trends = records
            .Where(r => r.PayloadJson != null)
            .Select(r => new
            {
                date = r.CreatedAt.Date.ToString("yyyy-MM-dd"),
                progress = ExtractProgress(r.PayloadJson)
            })
            .GroupBy(x => x.date)
            .Select(g => new
            {
                date = g.Key,
                averageProgress = g.Where(x => x.progress.HasValue).Average(x => x.progress!.Value)
            })
            .OrderBy(x => x.date)
            .ToList();

        return Ok(trends);
    }

    [HttpGet("api/v1/aums/charts/status-distribution")]
    public async Task<IActionResult> StatusDistribution(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(r => r.Module == "aums")
            .Where(r => r.PayloadJson != null)
            .ToListAsync(cancellationToken);

        var distribution = records
            .Select(r => ExtractStatus(r.PayloadJson))
            .Where(s => !string.IsNullOrEmpty(s))
            .GroupBy(s => s)
            .Select(g => new
            {
                status = g.Key,
                count = g.Count(),
                color = GetStatusColor(g.Key!)
            })
            .ToList();

        return Ok(distribution);
    }

    [HttpGet("api/v1/aums/charts/workload-by-fleet")]
    public async Task<IActionResult> WorkloadByFleet(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(r => r.Module == "aums")
            .Where(r => r.PayloadJson != null)
            .ToListAsync(cancellationToken);

        var workload = records
            .Select(r => ExtractFleetType(r.PayloadJson))
            .Where(f => !string.IsNullOrEmpty(f))
            .GroupBy(f => f)
            .Select(g => new
            {
                fleetType = g.Key,
                count = g.Count()
            })
            .ToList();

        return Ok(workload);
    }

    [HttpGet("api/v1/aums/charts/department-bottlenecks")]
    public async Task<IActionResult> DepartmentBottlenecks(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(r => r.Module == "aums")
            .Where(r => r.PayloadJson != null)
            .ToListAsync(cancellationToken);

        // Count delayed/overdue items by department
        var bottlenecks = records
            .Where(r => IsDelayed(r.PayloadJson))
            .Select(r => ExtractDepartment(r.PayloadJson))
            .Where(d => !string.IsNullOrEmpty(d))
            .GroupBy(d => d)
            .Select(g => new
            {
                department = g.Key,
                bottleneckCount = g.Count()
            })
            .OrderByDescending(x => x.bottleneckCount)
            .ToList();

        return Ok(bottlenecks);
    }

    [HttpGet("api/v1/aums/charts/overdue-heatmap")]
    public async Task<IActionResult> OverdueHeatmap([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.OperationalRecords.AsNoTracking().Where(r => r.Module == "aums");
        
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);

        var records = await query.ToListAsync(cancellationToken);
        
        var heatmap = records
            .Where(r => r.PayloadJson != null)
            .Where(r => IsDelayed(r.PayloadJson))
            .Select(r => new
            {
                date = r.CreatedAt.Date.ToString("yyyy-MM-dd"),
                overdueCount = 1
            })
            .GroupBy(x => x.date)
            .Select(g => new
            {
                date = g.Key,
                overdueCount = g.Count()
            })
            .OrderBy(x => x.date)
            .ToList();

        return Ok(heatmap);
    }

    [HttpGet("api/v1/aums/charts/delay-trends")]
    public async Task<IActionResult> DelayTrends([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.OperationalRecords.AsNoTracking().Where(r => r.Module == "aums");
        
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);

        var records = await query.ToListAsync(cancellationToken);
        
        var trends = records
            .Where(r => r.PayloadJson != null)
            .Where(r => IsDelayed(r.PayloadJson))
            .Select(r => new
            {
                date = r.CreatedAt.Date.ToString("yyyy-MM-dd"),
                delayedCount = 1
            })
            .GroupBy(x => x.date)
            .Select(g => new
            {
                date = g.Key,
                delayedCount = g.Count()
            })
            .OrderBy(x => x.date)
            .ToList();

        return Ok(trends);
    }

    [HttpGet("api/v1/aums/charts/maintenance-duration")]
    public async Task<IActionResult> MaintenanceDuration(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(r => r.Module == "aums")
            .Where(r => r.PayloadJson != null)
            .ToListAsync(cancellationToken);

        var durations = records
            .Select(r => new
            {
                projectId = r.Id.ToString(),
                duration = CalculateDuration(r.PayloadJson, r.CreatedAt, r.UpdatedAt)
            })
            .Where(x => x.duration.HasValue)
            .Select(x => new
            {
                projectId = x.projectId,
                duration = x.duration!.Value
            })
            .ToList();

        return Ok(durations);
    }

    [HttpGet("api/v1/aums/reports")]
    public async Task<IActionResult> GetReports([FromQuery] string reportType, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.OperationalRecords.AsNoTracking().Where(r => r.Module == "aums");
        
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);

        var records = await query.OrderByDescending(r => r.CreatedAt).Take(1000).ToListAsync(cancellationToken);
        
        var reportData = records.Select(r => new
        {
            id = r.Id,
            resource = r.Resource,
            action = r.Action,
            status = r.Status,
            payload = JsonSerializer.Deserialize<object>(r.PayloadJson),
            createdAt = r.CreatedAt,
            updatedAt = r.UpdatedAt ?? r.CreatedAt
        }).ToList();

        return Ok(new
        {
            reportType = reportType,
            generatedAt = DateTime.UtcNow,
            recordCount = reportData.Count,
            dateRange = new { start = startDate, end = endDate },
            data = reportData
        });
    }

    [HttpGet("api/v1/aums/reports/export")]
    public async Task<IActionResult> ExportReport([FromQuery] string reportType, [FromQuery] string format, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.OperationalRecords.AsNoTracking().Where(r => r.Module == "aums");
        
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);

        var records = await query.OrderByDescending(r => r.CreatedAt).Take(1000).ToListAsync(cancellationToken);
        
        var exportData = records.Select(r => new
        {
            id = r.Id,
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
                reportType = reportType,
                format = format,
                exportDate = DateTime.UtcNow,
                recordCount = exportData.Count,
                dateRange = new { start = startDate, end = endDate },
                data = exportData
            });
        }

        // For PDF/Excel, return a placeholder response
        return Ok(new
        {
            message = "PDF and Excel export not yet implemented. Use format=json for data export.",
            reportType = reportType,
            format = format,
            exportDate = DateTime.UtcNow,
            recordCount = exportData.Count,
            data = exportData
        });
    }

    // Helper methods for extracting data from JSON payloads
    private static double? ExtractProgress(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("progress", out var progress))
                return progress.GetDouble();
            if (payload.TryGetProperty("completionPercentage", out var completion))
                return completion.GetDouble();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractStatus(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("status", out var status))
                return status.GetString();
            if (payload.TryGetProperty("projectStatus", out var projectStatus))
                return projectStatus.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetStatusColor(string status)
    {
        return status?.ToLower() switch
        {
            "completed" => "#10b981",
            "in progress" => "#3b82f6",
            "pending" => "#f59e0b",
            "delayed" => "#ef4444",
            "cancelled" => "#6b7280",
            _ => "#9ca3af"
        };
    }

    private static string? ExtractFleetType(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("fleetType", out var fleetType))
                return fleetType.GetString();
            if (payload.TryGetProperty("aircraftType", out var aircraftType))
                return aircraftType.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractDepartment(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("department", out var department))
                return department.GetString();
            if (payload.TryGetProperty("section", out var section))
                return section.GetString();
            if (payload.TryGetProperty("sectionId", out var sectionId))
                return sectionId.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsDelayed(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("status", out var status))
            {
                var statusValue = status.GetString();
                return statusValue?.ToLower() == "delayed" || statusValue?.ToLower() == "overdue";
            }
            if (payload.TryGetProperty("isDelayed", out var isDelayed))
                return isDelayed.GetBoolean();
            if (payload.TryGetProperty("overdue", out var overdue))
                return overdue.GetBoolean();
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static int? CalculateDuration(string payloadJson, DateTimeOffset createdAt, DateTimeOffset? updatedAt)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            
            // Try to get duration from payload
            if (payload.TryGetProperty("duration", out var duration))
                return duration.GetInt32();
            
            // Calculate from dates
            if (payload.TryGetProperty("startDate", out var startDate) && payload.TryGetProperty("endDate", out var endDate))
            {
                var start = startDate.GetDateTime();
                var end = endDate.GetDateTime();
                return (int)(end - start).TotalDays;
            }
            
            // Calculate from record timestamps
            if (updatedAt.HasValue)
            {
                return (int)(updatedAt.Value - createdAt).TotalDays;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
}
