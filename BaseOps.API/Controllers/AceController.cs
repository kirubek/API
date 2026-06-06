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
public sealed class AceController(BaseOpsDbContext dbContext) : ControllerBase
{
    private static readonly string[] ValidActivityTypes = ["5S+1", "5s-plus-one", "fives-plus-one", "QCPC", "qcpc", "EHS", "ehs"];
    private static readonly string[] ValidStatuses = ["Draft", "Submitted", "Approved", "Rejected", "ReturnedForCorrection"];

    [HttpGet("api/ace/dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (currentUser is null) return Unauthorized();

        var query = dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "ace" || x.Module == "ace-activities");

        // Apply RBAC filtering
        switch (currentUser.Role)
        {
            case UserRole.Employee:
                query = query.Where(x => x.CreatedBy == userId);
                break;
            case UserRole.TeamLeader:
                if (currentUser.HangarId.HasValue)
                {
                    query = query.Where(x => x.PayloadJson != null &&
                        (x.PayloadJson.Contains($"\"hangarId\":\"{currentUser.HangarId}\"") ||
                         x.CreatedBy == userId));
                }
                else if (currentUser.ShopId.HasValue)
                {
                    query = query.Where(x => x.PayloadJson != null &&
                        (x.PayloadJson.Contains($"\"shopId\":\"{currentUser.ShopId}\"") ||
                         x.CreatedBy == userId));
                }
                else
                {
                    query = query.Where(x => x.CreatedBy == userId);
                }
                break;
            case UserRole.Manager:
                if (currentUser.SectionId.HasValue)
                {
                    query = query.Where(x => x.PayloadJson != null &&
                        x.PayloadJson.Contains($"\"sectionId\":\"{currentUser.SectionId}\""));
                }
                break;
            case UserRole.Director:
            case UserRole.SystemAdmin:
                // See all
                break;
            default:
                query = query.Where(x => x.CreatedBy == userId);
                break;
        }

        var records = await query.ToListAsync(cancellationToken);

        var dashboard = new
        {
            totalActivities = records.Count,
            draft = records.Count(r => r.Status == "Draft"),
            submitted = records.Count(r => r.Status == "Submitted"),
            approved = records.Count(r => r.Status == "Approved"),
            rejected = records.Count(r => r.Status == "Rejected"),
            returnedForCorrection = records.Count(r => r.Status == "ReturnedForCorrection"),
            fiveSPlusOne = records.Count(r => IsActivityType(r.PayloadJson, "5S+1") || IsActivityType(r.PayloadJson, "fives-plus-one")),
            qcpc = records.Count(r => IsActivityType(r.PayloadJson, "QCPC") || IsActivityType(r.PayloadJson, "qcpc")),
            ehs = records.Count(r => IsActivityType(r.PayloadJson, "EHS") || IsActivityType(r.PayloadJson, "ehs")),
            byStatus = new
            {
                draft = records.Count(r => r.Status == "Draft"),
                submitted = records.Count(r => r.Status == "Submitted"),
                approved = records.Count(r => r.Status == "Approved"),
                rejected = records.Count(r => r.Status == "Rejected"),
                returnedForCorrection = records.Count(r => r.Status == "ReturnedForCorrection")
            },
            byType = new
            {
                fiveSPlusOne = records.Count(r => IsActivityType(r.PayloadJson, "5S+1") || IsActivityType(r.PayloadJson, "fives-plus-one")),
                qcpc = records.Count(r => IsActivityType(r.PayloadJson, "QCPC") || IsActivityType(r.PayloadJson, "qcpc")),
                ehs = records.Count(r => IsActivityType(r.PayloadJson, "EHS") || IsActivityType(r.PayloadJson, "ehs"))
            }
        };

        return Ok(dashboard);
    }

    [HttpGet("api/ace-activities/list")]
    public async Task<IActionResult> GetActivities([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? activityType = null, [FromQuery] string? status = null, [FromQuery] Guid? sectionId = null, [FromQuery] Guid? hangarId = null, [FromQuery] Guid? shopId = null, [FromQuery] Guid? teamLeaderId = null, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .Include(x => x.Section)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (currentUser is null) return Unauthorized();

        var query = dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "ace" || x.Module == "ace-activities");

        // Log all records before filtering for debugging
        var allRecords = await query.ToListAsync(cancellationToken);
        Console.WriteLine($"[ACE] Total records in database: {allRecords.Count}");
        foreach (var record in allRecords)
        {
            Console.WriteLine($"[ACE] Record {record.Id}: Module={record.Module}, Status={record.Status}, CreatedBy={record.CreatedBy}, PayloadJson={record.PayloadJson?.Substring(0, Math.Min(150, record.PayloadJson?.Length ?? 0))}");
        }

        // RBAC: Apply role-based filtering
        Console.WriteLine($"[ACE] User {userId} with role {currentUser.Role} requesting activities. HangarId: {currentUser.HangarId}, ShopId: {currentUser.ShopId}, SectionId: {currentUser.SectionId}");

        switch (currentUser.Role)
        {
            case UserRole.Employee:
                // Employees see only their own submissions
                query = query.Where(x => x.CreatedBy == userId);
                Console.WriteLine($"[ACE] Employee filter: CreatedBy == {userId}");
                break;

            case UserRole.TeamLeader:
                // Team Leaders see submissions in their hangar or shop
                if (currentUser.HangarId.HasValue)
                {
                    query = query.Where(x => x.PayloadJson != null &&
                        (x.PayloadJson.Contains($"\"hangarId\":\"{currentUser.HangarId}\"") ||
                         x.CreatedBy == userId));
                    Console.WriteLine($"[ACE] TeamLeader filter: hangarId == {currentUser.HangarId} OR CreatedBy == {userId}");
                }
                else if (currentUser.ShopId.HasValue)
                {
                    query = query.Where(x => x.PayloadJson != null &&
                        (x.PayloadJson.Contains($"\"shopId\":\"{currentUser.ShopId}\"") ||
                         x.CreatedBy == userId));
                    Console.WriteLine($"[ACE] TeamLeader filter: shopId == {currentUser.ShopId} OR CreatedBy == {userId}");
                }
                else
                {
                    query = query.Where(x => x.CreatedBy == userId);
                    Console.WriteLine($"[ACE] TeamLeader filter (no hangar/shop): CreatedBy == {userId}");
                }
                break;

            case UserRole.Manager:
                // Managers see submissions in their section
                if (currentUser.SectionId.HasValue)
                {
                    query = query.Where(x => x.PayloadJson != null &&
                        x.PayloadJson.Contains($"\"sectionId\":\"{currentUser.SectionId}\""));
                    Console.WriteLine($"[ACE] Manager filter: sectionId == {currentUser.SectionId}");
                }
                break;

            case UserRole.Director:
            case UserRole.SystemAdmin:
                // Directors and Admins see all (no filter)
                Console.WriteLine($"[ACE] Director/SystemAdmin: no filter applied");
                break;

            default:
                query = query.Where(x => x.CreatedBy == userId);
                Console.WriteLine($"[ACE] Default filter: CreatedBy == {userId}");
                break;
        }

        // Apply additional filters (only if user has permission)
        if (currentUser.Role == UserRole.Manager || currentUser.Role == UserRole.Director || currentUser.Role == UserRole.SystemAdmin)
        {
            if (sectionId.HasValue)
            {
                query = query.Where(x => x.PayloadJson != null && 
                    x.PayloadJson.Contains($"\"sectionId\":\"{sectionId}\""));
            }
            if (hangarId.HasValue)
            {
                query = query.Where(x => x.PayloadJson != null && 
                    x.PayloadJson.Contains($"\"hangarId\":\"{hangarId}\""));
            }
            if (shopId.HasValue)
            {
                query = query.Where(x => x.PayloadJson != null && 
                    x.PayloadJson.Contains($"\"shopId\":\"{shopId}\""));
            }
            if (teamLeaderId.HasValue)
            {
                query = query.Where(x => x.PayloadJson != null && 
                    x.PayloadJson.Contains($"\"teamLeaderId\":\"{teamLeaderId}\""));
            }
        }

        // Filter by activity type if specified
        if (!string.IsNullOrEmpty(activityType))
        {
            query = query.Where(x => x.PayloadJson != null && 
                (x.PayloadJson.Contains($"\"activityType\":\"{activityType}\"") ||
                 x.PayloadJson.Contains($"\"type\":\"{activityType}\"")));
        }

        // Filter by status if specified
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        Console.WriteLine($"[ACE] After filtering: {total} records match criteria");

        var records = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Log sample payloads for debugging
        foreach (var record in records)
        {
            Console.WriteLine($"[ACE] Record {record.Id}: Module={record.Module}, Status={record.Status}, CreatedBy={record.CreatedBy}, PayloadJson={record.PayloadJson?.Substring(0, Math.Min(200, record.PayloadJson?.Length ?? 0))}");
        }

        // Fetch all hangars, shops, and sections for name lookup
        var hangars = await dbContext.Hangars.AsNoTracking().ToListAsync(cancellationToken);
        var shops = await dbContext.Shops.AsNoTracking().ToListAsync(cancellationToken);
        var sections = await dbContext.Sections.AsNoTracking().ToListAsync(cancellationToken);

        var hangarDict = hangars.ToDictionary(h => h.Id, h => h.Name);
        var shopDict = shops.ToDictionary(s => s.Id, s => s.Name);
        var sectionDict = sections.ToDictionary(s => s.Id, s => s.Name);

        var items = records.Select(r =>
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(r.PayloadJson);
                var activityType = ExtractActivityType(r.PayloadJson) ?? ExtractActivityTypeFromResource(r.Resource);
                Console.WriteLine($"[ACE] Processing record {r.Id}, activityType: {activityType}, status: {r.Status}");

                // Get submitter user information
                var submitter = r.CreatedBy.HasValue ? dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Id == r.CreatedBy.Value) : null;

                // Parse hangarId
                Guid? hangarId = null;
                if (payload.TryGetProperty("hangarId", out var hid) && hid.ValueKind != JsonValueKind.Null)
                {
                    if (hid.ValueKind == JsonValueKind.String && Guid.TryParse(hid.GetString(), out var hGuid)) hangarId = hGuid;
                    else if (hid.ValueKind == JsonValueKind.Object && hid.TryGetGuid(out var hGuid2)) hangarId = hGuid2;
                }

                // Parse shopId
                Guid? shopId = null;
                if (payload.TryGetProperty("shopId", out var sid) && sid.ValueKind != JsonValueKind.Null)
                {
                    if (sid.ValueKind == JsonValueKind.String && Guid.TryParse(sid.GetString(), out var sGuid)) shopId = sGuid;
                    else if (sid.ValueKind == JsonValueKind.Object && sid.TryGetGuid(out var sGuid2)) shopId = sGuid2;
                }

                // Parse sectionId
                Guid? sectionId = null;
                if (payload.TryGetProperty("sectionId", out var secid) && secid.ValueKind != JsonValueKind.Null)
                {
                    if (secid.ValueKind == JsonValueKind.String && Guid.TryParse(secid.GetString(), out var secGuid)) sectionId = secGuid;
                    else if (secid.ValueKind == JsonValueKind.Object && secid.TryGetGuid(out var secGuid2)) sectionId = secGuid2;
                }

                // Get hangar/shop/section names from dictionaries
                string? hangarName = hangarId.HasValue && hangarDict.TryGetValue(hangarId.Value, out var hName) ? hName : null;
                string? shopName = shopId.HasValue && shopDict.TryGetValue(shopId.Value, out var sName) ? sName : null;
                string? sectionName = sectionId.HasValue && sectionDict.TryGetValue(sectionId.Value, out var secName) ? secName : null;

                return new
                {
                    id = r.Id,
                    activityType = activityType,
                    status = r.Status,
                    activityDate = payload.TryGetProperty("activityDate", out var ad) ? ad.GetString() : r.CreatedAt.ToString("yyyy-MM-dd"),
                    checkedLocation = payload.TryGetProperty("checkedLocation", out var cl) ? cl.GetString() : "",
                    issueArea = payload.TryGetProperty("issueArea", out var ia) ? ia.GetString() : "",
                    auditedArea = payload.TryGetProperty("auditedArea", out var aa) ? aa.GetString() : "",
                    teamLeaderName = payload.TryGetProperty("teamLeaderName", out var tln) ? tln.GetString() : "",
                    hangarId = hangarId,
                    hangarName = hangarName,
                    shopId = shopId,
                    shopName = shopName,
                    sectionId = sectionId,
                    sectionName = sectionName,
                    submittedByUserId = r.CreatedBy?.ToString(),
                    submittedByUser = submitter != null ? new
                    {
                        id = submitter.Id,
                        fullName = submitter.FullName,
                        employeeId = submitter.EmployeeId
                    } : null,
                    submittedByName = submitter?.FullName,
                    createdAt = r.CreatedAt,
                    updatedAt = r.UpdatedAt ?? r.CreatedAt
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ACE] ERROR processing record {r.Id}: {ex.Message}");
                return null;
            }
        }).Where(x => x is not null).ToList();

        Console.WriteLine($"[ACE] Returning {items.Count} activities out of {total} total");

        return Ok(new { items, total, pageNumber, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) });
    }

    [HttpPost("api/ace-activities/5s-plus-one")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Create5SPlusOne([FromBody] JsonElement payload, CancellationToken cancellationToken = default)
    {
        return await CreateActivity("5S+1", payload, cancellationToken);
    }

    [HttpPost("api/ace-activities/qcpc")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> CreateQCPC([FromBody] JsonElement payload, CancellationToken cancellationToken = default)
    {
        return await CreateActivity("QCPC", payload, cancellationToken);
    }

    [HttpPost("api/ace-activities/ehs")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> CreateEHS([FromBody] JsonElement payload, CancellationToken cancellationToken = default)
    {
        return await CreateActivity("EHS", payload, cancellationToken);
    }

    private async Task<IActionResult> CreateActivity(string activityType, JsonElement payload, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .Include(x => x.Section)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (currentUser is null) return Unauthorized();

        // RBAC: Only Employees and Team Leaders can create ACE activities
        if (currentUser.Role != UserRole.Employee && 
            currentUser.Role != UserRole.TeamLeader)
        {
            return Forbid("Only Employees and Team Leaders can create ACE activities");
        }

        // Build the full payload with auto-filled fields
        var fullPayload = new
        {
            activityType,
            status = "Draft",
            sectionId = currentUser.SectionId,
            hangarId = currentUser.HangarId,
            shopId = currentUser.ShopId,
            teamLeaderId = userId,
            teamLeaderName = currentUser.FullName,
            submittedBy = currentUser.FullName,
            createdAt = DateTime.UtcNow
        };

        // Merge with the provided payload
        var payloadDict = JsonSerializer.Deserialize<Dictionary<string, object>>(payload.GetRawText());
        var fullPayloadDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(fullPayload));

        if (payloadDict != null && fullPayloadDict != null)
        {
            foreach (var kvp in payloadDict)
            {
                fullPayloadDict[kvp.Key] = kvp.Value;
            }
        }

        var record = new OperationalRecord
        {
            Module = "ace-activities",
            Resource = activityType.ToLower(),
            Action = "create",
            PayloadJson = JsonSerializer.Serialize(fullPayloadDict),
            Status = "Draft",
            CreatedBy = userId
        };

        dbContext.OperationalRecords.Add(record);
        var changes = await dbContext.SaveChangesAsync(cancellationToken);

        Console.WriteLine($"[ACE] Created activity: {record.Id}, Type: {activityType}, Changes: {changes}");

        return Ok(new
        {
            id = record.Id,
            activityType,
            status = "Draft",
            createdAt = record.CreatedAt
        });
    }

    [HttpPost("api/ace-activities/legacy/{id}/approve")]
    public async Task<IActionResult> ApproveActivity(Guid id, [FromBody] JsonElement? payload = null, CancellationToken cancellationToken = default)
    {
        return await UpdateActivityStatus(id, "Approved", payload, cancellationToken);
    }

    [HttpPost("api/ace-activities/legacy/{id}/reject")]
    public async Task<IActionResult> RejectActivity(Guid id, [FromBody] JsonElement? payload = null, CancellationToken cancellationToken = default)
    {
        return await UpdateActivityStatus(id, "Rejected", payload, cancellationToken);
    }

    [HttpPost("api/ace-activities/legacy/{id}/return-correction")]
    public async Task<IActionResult> ReturnForCorrection(Guid id, [FromBody] JsonElement? payload = null, CancellationToken cancellationToken = default)
    {
        return await UpdateActivityStatus(id, "ReturnedForCorrection", payload, cancellationToken);
    }

    // Update endpoints for each activity type
    [HttpPut("api/ace-activities/qcpc/{id}")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> UpdateQCPC(Guid id, [FromBody] JsonElement payload, CancellationToken cancellationToken = default)
    {
        return await UpdateActivity(id, payload, cancellationToken);
    }

    [HttpPut("api/ace-activities/ehs/{id}")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> UpdateEHS(Guid id, [FromBody] JsonElement payload, CancellationToken cancellationToken = default)
    {
        return await UpdateActivity(id, payload, cancellationToken);
    }

    // Submit endpoints for each activity type
    [HttpPost("api/ace-activities/qcpc/{id}/submit")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> SubmitQCPC(Guid id, CancellationToken cancellationToken = default)
    {
        return await SubmitActivity(id, cancellationToken);
    }

    [HttpPost("api/ace-activities/ehs/{id}/submit")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> SubmitEHS(Guid id, CancellationToken cancellationToken = default)
    {
        return await SubmitActivity(id, cancellationToken);
    }

    // Delete endpoints for each activity type
    [HttpDelete("api/ace-activities/qcpc/{id}")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> DeleteQCPC(Guid id, CancellationToken cancellationToken = default)
    {
        return await DeleteActivity(id, cancellationToken);
    }

    [HttpDelete("api/ace-activities/ehs/{id}")]
    [Authorize(Roles = "Employee,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> DeleteEHS(Guid id, CancellationToken cancellationToken = default)
    {
        return await DeleteActivity(id, cancellationToken);
    }

    private async Task<IActionResult> DeleteActivity(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && (x.Module == "ace" || x.Module == "ace-activities"), cancellationToken);

        if (record is null) return NotFound();

        // RBAC: Only the creator can delete, and only if status is Draft
        if (record.CreatedBy != userId)
        {
            return Forbid("You can only delete your own submissions");
        }

        if (record.Status != "Draft")
        {
            return Forbid("Only Draft forms can be deleted");
        }

        dbContext.OperationalRecords.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Activity deleted successfully" });
    }

    private async Task<IActionResult> UpdateActivity(Guid id, JsonElement payload, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (currentUser is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && (x.Module == "ace" || x.Module == "ace-activities"), cancellationToken);

        if (record is null) return NotFound();

        // RBAC: Only the creator can edit, and only if status is Draft
        if (record.CreatedBy != userId)
        {
            return Forbid("You can only edit your own submissions");
        }

        if (record.Status != "Draft")
        {
            return Forbid("Only Draft forms can be edited");
        }

        // Merge payload with existing data
        var currentPayload = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
        var currentDict = JsonSerializer.Deserialize<Dictionary<string, object>>(currentPayload.GetRawText());
        var newDict = JsonSerializer.Deserialize<Dictionary<string, object>>(payload.GetRawText());

        if (currentDict != null && newDict != null)
        {
            foreach (var kvp in newDict)
            {
                currentDict[kvp.Key] = kvp.Value;
            }

            record.PayloadJson = JsonSerializer.Serialize(currentDict);
            record.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { id = record.Id, updatedAt = record.UpdatedAt });
    }

    private async Task<IActionResult> SubmitActivity(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (currentUser is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && (x.Module == "ace" || x.Module == "ace-activities"), cancellationToken);

        if (record is null) return NotFound();

        Console.WriteLine($"[ACE] Submit activity: {id}, Current Status: {record.Status}, CreatedBy: {record.CreatedBy}, UserId: {userId}");

        // RBAC: Only the creator can submit
        if (record.CreatedBy != userId)
        {
            Console.WriteLine($"[ACE] Submit failed: User {userId} is not the creator {record.CreatedBy}");
            return Forbid("You can only submit your own submissions");
        }

        // Can only submit Draft forms
        if (record.Status != "Draft")
        {
            Console.WriteLine($"[ACE] Submit failed: Status is {record.Status}, not Draft");
            return BadRequest("Only Draft forms can be submitted");
        }

        record.Status = "Submitted";
        record.UpdatedAt = DateTimeOffset.UtcNow;

        var changes = await dbContext.SaveChangesAsync(cancellationToken);

        Console.WriteLine($"[ACE] Submitted activity: {id}, Changes: {changes}");

        return Ok(new { id = record.Id, status = "Submitted", updatedAt = record.UpdatedAt });
    }

    private async Task<IActionResult> UpdateActivityStatus(Guid id, string newStatus, JsonElement? payload, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .Include(x => x.Section)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (currentUser is null) return Unauthorized();

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && (x.Module == "ace" || x.Module == "ace-activities"), cancellationToken);

        if (record is null) return NotFound();

        Console.WriteLine($"[ACE] Review activity: {id}, NewStatus: {newStatus}, CurrentStatus: {record.Status}, User: {userId}, Role: {currentUser.Role}");

        // RBAC: Only Team Leaders, Managers, Directors, and System Admins can review
        if (currentUser.Role != UserRole.TeamLeader &&
            currentUser.Role != UserRole.Manager &&
            currentUser.Role != UserRole.Director &&
            currentUser.Role != UserRole.SystemAdmin)
        {
            Console.WriteLine($"[ACE] Review failed: User role {currentUser.Role} not authorized to review");
            return Forbid("Only Team Leaders, Managers, Directors, and System Admins can review ACE activities");
        }

        // Team Leaders can only review submissions in their hangar or shop
        if (currentUser.Role == UserRole.TeamLeader)
        {
            var activitySectionId = ExtractGuidFromPayload(record.PayloadJson, "sectionId");
            var activityHangarId = ExtractGuidFromPayload(record.PayloadJson, "hangarId");
            var activityShopId = ExtractGuidFromPayload(record.PayloadJson, "shopId");

            Console.WriteLine($"[ACE] TeamLeader scope check: UserHangarId={currentUser.HangarId}, UserShopId={currentUser.ShopId}, ActivityHangarId={activityHangarId}, ActivityShopId={activityShopId}");

            bool isInScope = false;
            if (currentUser.HangarId.HasValue && activityHangarId == currentUser.HangarId)
            {
                isInScope = true;
            }
            else if (currentUser.ShopId.HasValue && activityShopId == currentUser.ShopId)
            {
                isInScope = true;
            }

            if (!isInScope)
            {
                Console.WriteLine($"[ACE] Review failed: Team Leader not in scope");
                return Forbid("Team Leaders can only review submissions in their hangar or shop");
            }
        }

        // Managers can only review submissions in their section
        if (currentUser.Role == UserRole.Manager)
        {
            var activitySectionId = ExtractGuidFromPayload(record.PayloadJson, "sectionId");
            Console.WriteLine($"[ACE] Manager scope check: UserSectionId={currentUser.SectionId}, ActivitySectionId={activitySectionId}");
            if (activitySectionId != currentUser.SectionId)
            {
                Console.WriteLine($"[ACE] Review failed: Manager not in section scope");
                return Forbid("Managers can only review submissions in their section");
            }
        }

        // Can only review Submitted forms
        if (record.Status != "Submitted")
        {
            Console.WriteLine($"[ACE] Review failed: Status is {record.Status}, not Submitted");
            return BadRequest("Only Submitted forms can be reviewed");
        }

        // Update status
        record.Status = newStatus;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        // Add review comments if provided
        if (payload.HasValue)
        {
            try
            {
                var currentPayload = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
                var payloadDict = JsonSerializer.Deserialize<Dictionary<string, object>>(currentPayload.GetRawText());

                if (payloadDict != null)
                {
                    if (payload.Value.TryGetProperty("comments", out var comments))
                    {
                        payloadDict["reviewComments"] = comments.GetString() ?? "";
                    }
                    if (payload.Value.TryGetProperty("reason", out var reason))
                    {
                        payloadDict["reviewReason"] = reason.GetString() ?? "";
                    }
                    if (payload.Value.TryGetProperty("reviewedBy", out var reviewedBy))
                    {
                        payloadDict["reviewedBy"] = reviewedBy.GetString() ?? currentUser.FullName ?? "";
                    }
                    else
                    {
                        payloadDict["reviewedBy"] = currentUser.FullName ?? "";
                    }
                    payloadDict["reviewedAt"] = DateTime.UtcNow;
                    payloadDict["reviewedByUserId"] = userId?.ToString() ?? "";

                    record.PayloadJson = JsonSerializer.Serialize(payloadDict);
                }
            }
            catch
            {
                // If payload parsing fails, just update status
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = record.Id,
            status = newStatus,
            updatedAt = record.UpdatedAt
        });
    }

    private static Guid? ExtractGuidFromPayload(string payloadJson, string propertyName)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null)
            {
                return value.GetGuid();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
    [HttpGet("api/ace-activities/legacy/reports/monthly")]
    public async Task<IActionResult> GetMonthlyReport([FromQuery] string sectionId, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var records = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(r => r.Module == "ace" || r.Module == "ace-activities")
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            .Where(r => r.PayloadJson != null)
            .ToListAsync(cancellationToken);

        // Parse activities by type
        var fiveSPlusOne = records.Where(r => IsActivityType(r.PayloadJson, "fives-plus-one")).ToList();
        var qcpc = records.Where(r => IsActivityType(r.PayloadJson, "qcpc")).ToList();
        var ehs = records.Where(r => IsActivityType(r.PayloadJson, "ehs")).ToList();

        var report = new AceMonthlyReport
        {
            sectionId = sectionId,
            year = year,
            month = month,
            generatedAt = DateTime.UtcNow,
            totalActivities = records.Count,
            fiveSPlusOneCount = fiveSPlusOne.Count,
            qcpcCount = qcpc.Count,
            ehsCount = ehs.Count,
            completedCount = records.Count(r => IsStatus(r.PayloadJson, "Completed")),
            pendingCount = records.Count(r => IsStatus(r.PayloadJson, "Pending") || IsStatus(r.PayloadJson, "Submitted")),
            rejectedCount = records.Count(r => IsStatus(r.PayloadJson, "Rejected")),
            byActivityType = new
            {
                fiveSPlusOne = new
                {
                    total = fiveSPlusOne.Count,
                    completed = fiveSPlusOne.Count(r => IsStatus(r.PayloadJson, "Completed")),
                    pending = fiveSPlusOne.Count(r => IsStatus(r.PayloadJson, "Pending")),
                    rejected = fiveSPlusOne.Count(r => IsStatus(r.PayloadJson, "Rejected"))
                },
                qcpc = new
                {
                    total = qcpc.Count,
                    completed = qcpc.Count(r => IsStatus(r.PayloadJson, "Completed")),
                    pending = qcpc.Count(r => IsStatus(r.PayloadJson, "Pending")),
                    rejected = qcpc.Count(r => IsStatus(r.PayloadJson, "Rejected"))
                },
                ehs = new
                {
                    total = ehs.Count,
                    completed = ehs.Count(r => IsStatus(r.PayloadJson, "Completed")),
                    pending = ehs.Count(r => IsStatus(r.PayloadJson, "Pending")),
                    rejected = ehs.Count(r => IsStatus(r.PayloadJson, "Rejected"))
                }
            },
            activities = records.Select(r => new
            {
                id = r.Id,
                activityType = ExtractActivityType(r.PayloadJson),
                status = ExtractStatus(r.PayloadJson),
                submittedBy = ExtractSubmittedBy(r.PayloadJson),
                submittedAt = r.CreatedAt,
                findings = ExtractFindings(r.PayloadJson)
            }).ToList()
        };

        return Ok(report);
    }

    [HttpGet("api/ace-activities/legacy/reports/monthly/export")]
    public async Task<IActionResult> ExportMonthlyReport([FromQuery] string sectionId, [FromQuery] int year, [FromQuery] int month, [FromQuery] string format = "json", CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var records = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(r => r.Module == "ace" || r.Module == "ace-activities")
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            .Where(r => r.PayloadJson != null)
            .ToListAsync(cancellationToken);

        var reportData = records.Select(r => new
        {
            id = r.Id,
            activityType = ExtractActivityType(r.PayloadJson),
            status = ExtractStatus(r.PayloadJson),
            submittedBy = ExtractSubmittedBy(r.PayloadJson),
            submittedAt = r.CreatedAt,
            payload = JsonSerializer.Deserialize<object>(r.PayloadJson)
        }).ToList();

        if (format.ToLower() == "json")
        {
            return Ok(new
            {
                sectionId = sectionId,
                year = year,
                month = month,
                exportDate = DateTime.UtcNow,
                recordCount = reportData.Count,
                data = reportData
            });
        }

        // For PDF/Excel, return a placeholder response
        return Ok(new
        {
            message = "PDF and Excel export not yet implemented. Use format=json for data export.",
            sectionId = sectionId,
            year = year,
            month = month,
            format = format,
            exportDate = DateTime.UtcNow,
            recordCount = reportData.Count,
            data = reportData
        });
    }

    // Helper methods
    private static bool IsActivityType(string payloadJson, string activityType)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("activityType", out var at))
                return at.GetString()?.ToLower() == activityType.ToLower();
            if (payload.TryGetProperty("type", out var t))
                return t.GetString()?.ToLower() == activityType.ToLower();
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsStatus(string payloadJson, string status)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("status", out var s))
                return s.GetString()?.ToLower() == status.ToLower();
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string? ExtractActivityType(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("activityType", out var at))
                return at.GetString();
            if (payload.TryGetProperty("type", out var t))
                return t.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractActivityTypeFromResource(string? resource)
    {
        if (string.IsNullOrEmpty(resource))
            return null;

        return resource.ToLower() switch
        {
            "fives-plus-one" or "5s-plus-one" or "5s+1" => "5S+1",
            "qcpc" => "QCPC",
            "ehs" => "EHS",
            _ => null
        };
    }

    private static string? ExtractStatus(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("status", out var s))
                return s.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractSubmittedBy(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("submittedBy", out var sb))
                return sb.GetString();
            if (payload.TryGetProperty("createdBy", out var cb))
                return cb.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static int ExtractFindings(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            if (payload.TryGetProperty("findings", out var findings) && findings.ValueKind == JsonValueKind.Array)
                return findings.GetArrayLength();
            if (payload.TryGetProperty("totalFindings", out var tf))
                return tf.GetInt32();
            return 0;
        }
        catch
        {
            return 0;
        }
    }
}

public class AceMonthlyReport
{
    public string sectionId { get; set; } = string.Empty;
    public int year { get; set; }
    public int month { get; set; }
    public DateTime generatedAt { get; set; }
    public int totalActivities { get; set; }
    public int fiveSPlusOneCount { get; set; }
    public int qcpcCount { get; set; }
    public int ehsCount { get; set; }
    public int completedCount { get; set; }
    public int pendingCount { get; set; }
    public int rejectedCount { get; set; }
    public object byActivityType { get; set; } = new object();
    public object activities { get; set; } = new object();
}
