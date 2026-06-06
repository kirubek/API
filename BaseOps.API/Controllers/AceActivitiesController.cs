using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using BaseOps.Application.Bulletins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
public sealed class AceActivitiesController(BaseOpsDbContext dbContext, IAttachmentSecurityService attachmentService) : ControllerBase
{
    [HttpGet("api/ace-activities/fives-plus-one")]
    public async Task<IActionResult> ListFiveSPlusOne(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] Guid? sectionId = null,
        [FromQuery] Guid? hangarId = null,
        [FromQuery] Guid? shopId = null,
        [FromQuery] Guid? submittedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Unauthorized();

        var query = dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "ace-activities" && x.Resource == "fives-plus-one" && x.Status != "Deleted");

        // Apply RBAC filtering
        if (user.Role == UserRole.Employee)
        {
            // Employees see only their own submissions
            query = query.Where(x => x.CreatedBy == userId);
        }
        else if (user.Role == UserRole.TeamLeader)
        {
            var teamUserIds = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.ReportsToUserId == user.Id ||
                    (!u.ReportsToUserId.HasValue &&
                     ((user.HangarId.HasValue && u.HangarId == user.HangarId) ||
                      (user.ShopId.HasValue && u.ShopId == user.ShopId))))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(x => x.CreatedBy.HasValue && teamUserIds.Contains(x.CreatedBy.Value));
        }
        else if (user.Role == UserRole.Manager)
        {
            // Managers see submissions from their section
            if (user.SectionId.HasValue)
            {
                var sectionUserIds = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.SectionId == user.SectionId)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);
                query = query.Where(x => x.CreatedBy.HasValue && sectionUserIds.Contains(x.CreatedBy.Value));
            }
        }
        // Directors and Admins see all

        // Apply additional filters
        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);
        if (sectionId.HasValue)
            query = query.Where(x => x.PayloadJson != null && x.PayloadJson.Contains($"\"sectionId\":\"{sectionId}\""));
        if (hangarId.HasValue)
            query = query.Where(x => x.PayloadJson != null && x.PayloadJson.Contains($"\"hangarId\":\"{hangarId}\""));
        if (shopId.HasValue)
            query = query.Where(x => x.PayloadJson != null && x.PayloadJson.Contains($"\"shopId\":\"{shopId}\""));
        if (submittedByUserId.HasValue)
            query = query.Where(x => x.CreatedBy == submittedByUserId);

        var total = await query.CountAsync(cancellationToken);
        var records = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = records.Select(r =>
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(r.PayloadJson);
            var submitter = dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Id == r.CreatedBy);
            var reviewer = r.UpdatedBy.HasValue ? dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Id == r.UpdatedBy) : null;

            return new
            {
                id = r.Id,
                submittedByUserId = r.CreatedBy?.ToString(),
                submittedByUser = submitter != null ? new
                {
                    id = submitter.Id,
                    fullName = submitter.FullName,
                    employeeId = submitter.EmployeeId
                } : null,
                sectionId = payload.TryGetProperty("sectionId", out var sid) ? sid.GetString() : "",
                hangarId = payload.TryGetProperty("hangarId", out var hid) ? hid.GetString() : "",
                shopId = payload.TryGetProperty("shopId", out var shid) ? shid.GetString() : "",
                activityDate = payload.TryGetProperty("activityDate", out var ad) ? ad.GetString() : "",
                status = r.Status,
                submissionRemarks = payload.TryGetProperty("submissionRemarks", out var sr) ? sr.GetString() : "",
                submittedAt = r.CreatedAt,
                reviewedAt = r.UpdatedAt,
                reviewedByUserId = r.UpdatedBy?.ToString(),
                reviewedByUser = reviewer != null ? new
                {
                    id = reviewer.Id,
                    fullName = reviewer.FullName,
                    employeeId = reviewer.EmployeeId
                } : null,
                reviewRemarks = payload.TryGetProperty("reviewRemarks", out var rr) ? rr.GetString() : "",
                teamLeaderName = payload.TryGetProperty("teamLeaderName", out var tln) ? tln.GetString() : "",
                checkedLocation = payload.TryGetProperty("checkedLocation", out var cl) ? cl.GetString() : "",
                question1 = payload.TryGetProperty("question1", out var q1) ? q1.GetBoolean() : false,
                question2 = payload.TryGetProperty("question2", out var q2) ? q2.GetBoolean() : false,
                question3 = payload.TryGetProperty("question3", out var q3) ? q3.GetBoolean() : false,
                question4 = payload.TryGetProperty("question4", out var q4) ? q4.GetBoolean() : false,
                question5 = payload.TryGetProperty("question5", out var q5) ? q5.GetBoolean() : false,
                question6 = payload.TryGetProperty("question6", out var q6) ? q6.GetBoolean() : false,
                question7 = payload.TryGetProperty("question7", out var q7) ? q7.GetBoolean() : false,
                question8 = payload.TryGetProperty("question8", out var q8) ? q8.GetBoolean() : false,
                findingsOrComments = payload.TryGetProperty("findingsOrComments", out var foc) ? foc.GetString() : "",
                createdAt = r.CreatedAt,
                updatedAt = r.UpdatedAt ?? r.CreatedAt,
                rowVersion = r.Version
            };
        }).ToArray();

        return Ok(new { items, total, pageNumber, pageSize });
    }

    [HttpGet("api/ace-activities/fives-plus-one/{id}")]
    public async Task<IActionResult> GetFiveSPlusOne(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.OperationalRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities" && x.Resource == "fives-plus-one", cancellationToken);

        if (record is null) return NotFound();

        var payload = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
        var submitter = dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Id == record.CreatedBy);
        var reviewer = record.UpdatedBy.HasValue ? dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Id == record.UpdatedBy) : null;

        return Ok(new
        {
            id = record.Id,
            submittedByUserId = record.CreatedBy?.ToString(),
            submittedByUser = submitter != null ? new
            {
                id = submitter.Id,
                fullName = submitter.FullName,
                employeeId = submitter.EmployeeId
            } : null,
            sectionId = payload.TryGetProperty("sectionId", out var sid) ? sid.GetString() : "",
            hangarId = payload.TryGetProperty("hangarId", out var hid) ? hid.GetString() : "",
            shopId = payload.TryGetProperty("shopId", out var shid) ? shid.GetString() : "",
            activityDate = payload.TryGetProperty("activityDate", out var ad) ? ad.GetString() : "",
            status = record.Status,
            submissionRemarks = payload.TryGetProperty("submissionRemarks", out var sr) ? sr.GetString() : "",
            submittedAt = record.CreatedAt,
            reviewedAt = record.UpdatedAt,
            reviewedByUserId = record.UpdatedBy?.ToString(),
            reviewedByUser = reviewer != null ? new
            {
                id = reviewer.Id,
                fullName = reviewer.FullName,
                employeeId = reviewer.EmployeeId
            } : null,
            reviewRemarks = payload.TryGetProperty("reviewRemarks", out var rr) ? rr.GetString() : "",
            teamLeaderName = payload.TryGetProperty("teamLeaderName", out var tln) ? tln.GetString() : "",
            checkedLocation = payload.TryGetProperty("checkedLocation", out var cl) ? cl.GetString() : "",
            question1 = payload.TryGetProperty("question1", out var q1) ? q1.GetBoolean() : false,
            question2 = payload.TryGetProperty("question2", out var q2) ? q2.GetBoolean() : false,
            question3 = payload.TryGetProperty("question3", out var q3) ? q3.GetBoolean() : false,
            question4 = payload.TryGetProperty("question4", out var q4) ? q4.GetBoolean() : false,
            question5 = payload.TryGetProperty("question5", out var q5) ? q5.GetBoolean() : false,
            question6 = payload.TryGetProperty("question6", out var q6) ? q6.GetBoolean() : false,
            question7 = payload.TryGetProperty("question7", out var q7) ? q7.GetBoolean() : false,
            question8 = payload.TryGetProperty("question8", out var q8) ? q8.GetBoolean() : false,
            findingsOrComments = payload.TryGetProperty("findingsOrComments", out var foc) ? foc.GetString() : "",
            createdAt = record.CreatedAt,
            updatedAt = record.UpdatedAt ?? record.CreatedAt,
            rowVersion = record.Version
        });
    }

    [HttpGet("api/ace-activities/{id}")]
    public async Task<IActionResult> GetActivity(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities" && x.Status != "Deleted", cancellationToken);

        if (record is null) return NotFound();

        if (!await CanReadActivity(user, record, cancellationToken))
            return Forbid();

        var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(record.PayloadJson) ?? new Dictionary<string, object>();
        payload["id"] = record.Id;
        payload["activityType"] = record.Resource == "fives-plus-one" ? "5S+1" : record.Resource?.ToUpperInvariant() ?? "";
        payload["status"] = record.Status;
        payload["submittedByUserId"] = record.CreatedBy?.ToString() ?? "";
        payload["submittedAt"] = record.CreatedAt;
        payload["reviewedByUserId"] = record.UpdatedBy?.ToString() ?? "";
        payload["reviewedAt"] = record.UpdatedAt ?? record.CreatedAt;
        payload["createdAt"] = record.CreatedAt;
        payload["updatedAt"] = record.UpdatedAt ?? record.CreatedAt;
        payload["rowVersion"] = record.Version;

        return Ok(payload);
    }

    [HttpPost("api/ace-activities/fives-plus-one")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> CreateFiveSPlusOne([FromBody] JsonElement payload, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Unauthorized();

        // Add employee ID to payload
        var payloadObj = JsonSerializer.Deserialize<Dictionary<string, object>>(payload.GetRawText()) ?? new Dictionary<string, object>();
        payloadObj["submittedByUserId"] = userId.Value.ToString();
        payloadObj["submittedByEmployeeId"] = user.EmployeeId ?? "";
        payloadObj["sectionId"] = user.SectionId?.ToString() ?? "";
        payloadObj["hangarId"] = user.HangarId?.ToString() ?? "";
        payloadObj["shopId"] = user.ShopId?.ToString() ?? "";

        var record = new OperationalRecord
        {
            Id = Guid.NewGuid(),
            Module = "ace-activities",
            Resource = "fives-plus-one",
            Action = "create",
            PayloadJson = JsonSerializer.Serialize(payloadObj),
            Status = "Draft",
            CreatedBy = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.OperationalRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/ace-activities/fives-plus-one/{record.Id}", new { id = record.Id });
    }

    [HttpPut("api/ace-activities/fives-plus-one/{id}")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> UpdateFiveSPlusOne(Guid id, [FromBody] JsonElement payload, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities" && x.Resource == "fives-plus-one", cancellationToken);

        if (record is null) return NotFound();

        // Only allow submitter to update draft records
        if (record.Status != "Draft" || record.CreatedBy != userId)
            return BadRequest(new { error = "Only the submitter can update draft records" });

        var payloadObj = JsonSerializer.Deserialize<Dictionary<string, object>>(payload.GetRawText()) ?? new Dictionary<string, object>();
        payloadObj["submittedByUserId"] = userId.Value.ToString();
        
        record.PayloadJson = JsonSerializer.Serialize(payloadObj);
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { id = record.Id });
    }

    [HttpPost("api/ace-activities/fives-plus-one/{id}/submit")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> SubmitFiveSPlusOne(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities" && x.Resource == "fives-plus-one", cancellationToken);

        if (record is null) return NotFound();

        // Only allow submitter to submit
        if (record.CreatedBy != userId)
            return BadRequest(new { error = "Only the submitter can submit the form" });

        if (record.Status != "Draft")
            return BadRequest(new { error = "Only draft records can be submitted" });

        record.Status = "Submitted";
        record.Action = "submit";
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { id = record.Id, status = "Submitted" });
    }

    [HttpPost("api/ace-activities/review/{id}/approve")]
    public async Task<IActionResult> ApproveActivity(Guid id, [FromBody] JsonElement payload, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Unauthorized();

        if (user.Role != UserRole.TeamLeader)
            return Forbid();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities", cancellationToken);

        if (record is null) return NotFound();

        if (record.Resource != "qcpc")
            return BadRequest(new { error = "Only QCPC forms can be approved" });

        if (!await IsTeamLeaderInScope(user, record, cancellationToken))
            return Forbid();

        if (record.Status != "Submitted")
            return BadRequest(new { error = "Only submitted records can be approved" });

        if (payload.TryGetProperty("rowVersion", out var approveRowVersion) && approveRowVersion.TryGetUInt32(out var approveVersion) && approveVersion != record.Version)
            return Conflict(new { error = "This ACE activity was already modified. Refresh and try again." });

        var payloadObj = JsonSerializer.Deserialize<Dictionary<string, object>>(record.PayloadJson) ?? new Dictionary<string, object>();
        if (payload.TryGetProperty("comments", out var comments))
        {
            payloadObj["reviewComments"] = comments.GetString() ?? "";
        }
        if (payload.TryGetProperty("reviewRemarks", out var remarks))
        {
            payloadObj["reviewRemarks"] = remarks.GetString() ?? "";
        }
        payloadObj["reviewedBy"] = user.FullName ?? "";
        payloadObj["reviewedByUserId"] = userId.Value.ToString();
        payloadObj["reviewedAt"] = DateTimeOffset.UtcNow;
        record.PayloadJson = JsonSerializer.Serialize(payloadObj);

        record.Status = "Approved";
        record.Action = "approve";
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = userId;
        record.Version++;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { id = record.Id, status = "Approved" });
    }

    [HttpPost("api/ace-activities/review/{id}/reject")]
    public async Task<IActionResult> RejectActivity(Guid id, [FromBody] JsonElement payload, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Unauthorized();

        if (user.Role != UserRole.TeamLeader)
            return Forbid();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities", cancellationToken);

        if (record is null) return NotFound();

        if (record.Resource != "qcpc")
            return BadRequest(new { error = "Only QCPC forms can be rejected" });

        if (!await IsTeamLeaderInScope(user, record, cancellationToken))
            return Forbid();

        if (record.Status != "Submitted")
            return BadRequest(new { error = "Only submitted records can be rejected" });

        if (payload.TryGetProperty("rowVersion", out var rejectRowVersion) && rejectRowVersion.TryGetUInt32(out var rejectVersion) && rejectVersion != record.Version)
            return Conflict(new { error = "This ACE activity was already modified. Refresh and try again." });

        var reasonValue = payload.TryGetProperty("reason", out var reason) ? reason.GetString() : null;
        if (string.IsNullOrWhiteSpace(reasonValue))
            return BadRequest(new { error = "Rejection reason is required" });

        var payloadObj = JsonSerializer.Deserialize<Dictionary<string, object>>(record.PayloadJson) ?? new Dictionary<string, object>();
        payloadObj["reviewReason"] = reasonValue;
        if (payload.TryGetProperty("reviewRemarks", out var remarks))
        {
            payloadObj["reviewRemarks"] = remarks.GetString() ?? "";
        }
        payloadObj["reviewedBy"] = user.FullName ?? "";
        payloadObj["reviewedByUserId"] = userId.Value.ToString();
        payloadObj["reviewedAt"] = DateTimeOffset.UtcNow;
        record.PayloadJson = JsonSerializer.Serialize(payloadObj);

        record.Status = "Rejected";
        record.Action = "reject";
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = userId;
        record.Version++;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { id = record.Id, status = "Rejected" });
    }

    [HttpPost("api/ace-activities/review/{id}/return")]
    public async Task<IActionResult> ReturnForCorrection(Guid id, [FromBody] JsonElement payload, CancellationToken cancellationToken = default)
    {
        return BadRequest(new { error = "Return for correction is disabled. Use approve or reject." });
    }

    [HttpDelete("api/ace-activities/fives-plus-one/{id}")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> DeleteFiveSPlusOne(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities" && x.Resource == "fives-plus-one", cancellationToken);

        if (record is null) return NotFound();

        // Only allow submitter to delete draft records
        if (record.Status != "Draft" || record.CreatedBy != userId)
            return BadRequest(new { error = "Only the submitter can delete draft records" });

        record.Status = "Deleted";
        record.Action = "delete";
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { id = record.Id });
    }

    [HttpGet("api/ace-activities/reports/monthly")]
    public async Task<IActionResult> GetMonthlyReport(
        [FromQuery] Guid? sectionId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Unauthorized();

        var targetSectionId = sectionId ?? user.SectionId;
        if (targetSectionId is null)
            return BadRequest(new { error = "Section ID is required" });

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var query = dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "ace-activities" && x.Status == "Approved")
            .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate);

        // Apply RBAC filtering
        if (user.Role == UserRole.TeamLeader)
        {
            var teamUserIds = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.ReportsToUserId == user.Id ||
                    (!u.ReportsToUserId.HasValue &&
                     ((user.HangarId.HasValue && u.HangarId == user.HangarId) ||
                      (user.ShopId.HasValue && u.ShopId == user.ShopId))))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(x => x.CreatedBy.HasValue && teamUserIds.Contains(x.CreatedBy.Value));
        }
        else if (user.Role == UserRole.Manager)
        {
            var sectionUserIds = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.SectionId == targetSectionId)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(x => x.CreatedBy.HasValue && sectionUserIds.Contains(x.CreatedBy.Value));
        }

        var records = await query.ToListAsync(cancellationToken);

        // Count by employee
        var employeeCounts = new Dictionary<string, int>();
        foreach (var record in records)
        {
            if (record.CreatedBy.HasValue)
            {
                var key = record.CreatedBy.Value.ToString();
                employeeCounts[key] = employeeCounts.GetValueOrDefault(key, 0) + 1;
            }
        }

        // Get employee details
        var employeeIds = employeeCounts.Keys.Select(Guid.Parse).ToList();
        var employees = await dbContext.Users
            .AsNoTracking()
            .Where(u => employeeIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.EmployeeId,
                u.HangarId,
                u.ShopId
            })
            .ToListAsync(cancellationToken);

        var report = employees.Select(e => new
        {
            employeeId = e.EmployeeId,
            employeeName = e.FullName,
            hangarId = e.HangarId?.ToString(),
            shopId = e.ShopId?.ToString(),
            approvedActivityCount = employeeCounts[e.Id.ToString()]
        }).ToList();

        return Ok(new
        {
            sectionId = targetSectionId,
            year,
            month,
            totalApprovedActivities = records.Count,
            employeeReports = report
        });
    }

    private async Task<bool> IsTeamLeaderInScope(ApplicationUser user, OperationalRecord record, CancellationToken cancellationToken)
    {
        if (!record.CreatedBy.HasValue)
            return false;

        var submitter = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == record.CreatedBy.Value, cancellationToken);

        if (submitter is null)
            return false;

        if (submitter.ReportsToUserId == user.Id)
            return true;

        if (submitter.ReportsToUserId.HasValue)
            return false;

        if (submitter.HangarId.HasValue && user.HangarId.HasValue && submitter.HangarId == user.HangarId)
            return true;

        if (submitter.ShopId.HasValue && user.ShopId.HasValue && submitter.ShopId == user.ShopId)
            return true;

        return false;
    }

    private async Task<bool> CanReadActivity(ApplicationUser user, OperationalRecord record, CancellationToken cancellationToken)
    {
        if (user.Role == UserRole.Director || user.Role == UserRole.SystemAdmin)
            return true;

        if (!record.CreatedBy.HasValue)
            return false;

        if (user.Role == UserRole.Employee)
            return record.CreatedBy == user.Id;

        if (user.Role == UserRole.TeamLeader)
            return await IsTeamLeaderInScope(user, record, cancellationToken);

        if (user.Role == UserRole.Manager)
        {
            if (!user.SectionId.HasValue)
                return false;

            var submitter = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == record.CreatedBy.Value, cancellationToken);

            return submitter?.SectionId == user.SectionId;
        }

        return false;
    }

    [HttpPost("api/ace-activities/{id}/attachment")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities", cancellationToken);

        if (record is null) return NotFound();

        // Only allow creator to upload attachments to draft records
        if (record.Status != "Draft" || record.CreatedBy != userId)
            return BadRequest(new { error = "Only the creator can upload attachments to draft records" });

        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        // Read file content
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileContent = memoryStream.ToArray();

        // Store attachment securely
        var (generatedFileName, storagePath) = await attachmentService.StoreAttachmentAsync(
            file.FileName,
            file.ContentType,
            fileContent,
            userId.Value,
            cancellationToken);

        // Save attachment to database
        var attachment = new AceAttachment
        {
            Id = Guid.NewGuid(),
            ActivityId = id,
            OriginalFileName = file.FileName,
            GeneratedFileName = generatedFileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            StoragePath = storagePath,
            UploadedBy = userId.Value,
            UploadTimestamp = DateTime.UtcNow
        };

        dbContext.AceAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = attachment.Id,
            originalFileName = file.FileName,
            generatedFileName = generatedFileName,
            contentType = file.ContentType,
            fileSize = file.Length
        });
    }

    [HttpGet("api/ace-activities/{id}/attachments")]
    public async Task<IActionResult> ListAttachments(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities", cancellationToken);

        if (record is null) return NotFound();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Unauthorized();

        // Check read access
        if (!await CanReadActivity(user, record, cancellationToken))
            return Forbid();

        var attachments = await dbContext.AceAttachments
            .AsNoTracking()
            .Where(a => a.ActivityId == id)
            .ToListAsync(cancellationToken);

        var attachmentDtos = attachments.Select(a => new
        {
            id = a.Id,
            originalFileName = a.OriginalFileName,
            contentType = a.ContentType,
            fileSize = a.FileSize,
            uploadedBy = a.UploadedBy,
            uploadTimestamp = a.UploadTimestamp,
            downloadUrl = $"/api/ace-activities/{id}/attachment/{a.Id}"
        }).ToList();

        return Ok(attachmentDtos);
    }

    [HttpGet("api/ace-activities/{id}/attachment/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities", cancellationToken);

        if (record is null) return NotFound();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Unauthorized();

        // Check read access
        if (!await CanReadActivity(user, record, cancellationToken))
            return Forbid();

        var attachment = await dbContext.AceAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.ActivityId == id, cancellationToken);

        if (attachment == null) return NotFound();

        var fileContent = await attachmentService.GetAttachmentContentAsync(attachment.StoragePath, cancellationToken);
        if (fileContent == null) return NotFound();

        return File(fileContent, attachment.ContentType, attachment.OriginalFileName);
    }

    [HttpDelete("api/ace-activities/{id}/attachment/{attachmentId:guid}")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "ace-activities", cancellationToken);

        if (record is null) return NotFound();

        // Only allow creator to delete attachments from draft records
        if (record.Status != "Draft" || record.CreatedBy != userId)
            return BadRequest(new { error = "Only the creator can delete attachments from draft records" });

        var attachment = await dbContext.AceAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.ActivityId == id, cancellationToken);

        if (attachment == null) return NotFound();

        dbContext.AceAttachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Attachment deleted successfully" });
    }
}
