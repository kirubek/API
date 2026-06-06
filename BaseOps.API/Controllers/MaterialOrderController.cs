using BaseOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
public sealed class MaterialOrderController(BaseOpsDbContext dbContext) : ControllerBase
{
    [HttpGet("api/material-order-statuses/statistics")]
    [HttpGet("api/material-order-status/statistics")]
    public async Task<IActionResult> Statistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.OperationalRecords.AsNoTracking().Where(r => r.Module == "mo" || r.Module == "material-order");
        
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);

        var records = await query.ToListAsync(cancellationToken);
        
        var statistics = new
        {
            totalOrders = records.Count,
            pending = records.Count(r => r.Status == "Pending" || r.Status == "Submitted"),
            approved = records.Count(r => r.Status == "Approved"),
            rejected = records.Count(r => r.Status == "Rejected"),
            escalated = records.Count(r => r.Status == "Escalated"),
            completed = records.Count(r => r.Status == "Completed"),
            averageProcessingDays = CalculateAverageProcessingDays(records)
        };

        return Ok(statistics);
    }

    [HttpGet("api/material-order-statuses/analytics")]
    [HttpGet("api/material-order-status/analytics")]
    public async Task<IActionResult> Analytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.OperationalRecords.AsNoTracking().Where(r => r.Module == "mo" || r.Module == "material-order");
        
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);

        var records = await query.ToListAsync(cancellationToken);
        
        var analytics = new
        {
            byStatus = records
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToList(),
            
            byPriority = records
                .Where(r => r.PayloadJson != null)
                .Select(r => ExtractPriority(r.PayloadJson))
                .Where(p => !string.IsNullOrEmpty(p))
                .GroupBy(p => p)
                .Select(g => new { priority = g.Key, count = g.Count() })
                .ToList(),
            
            bySection = records
                .Where(r => r.PayloadJson != null)
                .Select(r => ExtractSection(r.PayloadJson))
                .Where(s => !string.IsNullOrEmpty(s))
                .GroupBy(s => s)
                .Select(g => new { section = g.Key, count = g.Count() })
                .ToList(),
            
            monthlyTrends = records
                .GroupBy(r => new { year = r.CreatedAt.Year, month = r.CreatedAt.Month })
                .Select(g => new
                {
                    year = g.Key.year,
                    month = g.Key.month,
                    count = g.Count()
                })
                .OrderBy(x => x.year).ThenBy(x => x.month)
                .ToList()
        };

        return Ok(analytics);
    }

    [HttpGet("api/material-order-statuses/dashboard")]
    [HttpGet("api/material-order-status/dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(r => r.Module == "mo" || r.Module == "material-order")
            .Where(r => r.PayloadJson != null)
            .ToListAsync(cancellationToken);

        var dashboard = new
        {
            totalOrders = records.Count,
            pendingOrders = records.Count(r => r.Status == "Pending" || r.Status == "Submitted"),
            approvedOrders = records.Count(r => r.Status == "Approved"),
            rejectedOrders = records.Count(r => r.Status == "Rejected"),
            escalatedOrders = records.Count(r => r.Status == "Escalated"),
            criticalOrders = records.Count(r => IsCritical(r.PayloadJson)),
            byStatus = records
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToList(),
            recentOrders = records
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new
                {
                    id = r.Id,
                    orderNumber = ExtractOrderNumber(r.PayloadJson),
                    status = r.Status,
                    createdAt = r.CreatedAt,
                    priority = ExtractPriority(r.PayloadJson)
                })
                .ToList()
        };

        return Ok(dashboard);
    }

    // Helper methods
    private static double? CalculateAverageProcessingDays(List<BaseOps.Domain.Entities.OperationalRecord> records)
    {
        var completedRecords = records
            .Where(r => r.Status == "Completed" && r.UpdatedAt.HasValue)
            .ToList();

        if (!completedRecords.Any())
            return null;

        var totalDays = completedRecords.Sum(r => (r.UpdatedAt!.Value - r.CreatedAt).TotalDays);
        return totalDays / completedRecords.Count;
    }

    private static string? ExtractPriority(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("priority", out var priority))
                return priority.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractSection(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
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

    private static string? ExtractOrderNumber(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("orderNumber", out var orderNumber))
                return orderNumber.GetString();
            if (payload.TryGetProperty("id", out var id))
                return id.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsCritical(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("priority", out var priority))
            {
                var priorityValue = priority.GetString();
                return priorityValue?.ToLower() == "critical" || priorityValue?.ToLower() == "high";
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
