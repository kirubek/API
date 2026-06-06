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
[Route("api")]
[Authorize]
public sealed class ScheduleController(BaseOpsDbContext dbContext) : ControllerBase
{
    private static readonly string[] ValidShiftTypes = ["Day", "Night", "Evening", "LongNight", "DayOff", "Vacation"];

    [HttpPost("monthly-schedules")]
    public async Task<IActionResult> SaveMonthlySchedule([FromBody] SaveScheduleRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users
            .Include(x => x.Hangar)
            .Include(x => x.Section)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        
        if (currentUser is null) return Unauthorized();
        
        // Only Team Leaders can create schedules
        if (currentUser.Role != UserRole.TeamLeader)
            return Forbid("Only Team Leaders can create monthly schedules");

        // Validate user has hangar or shop assignment
        if (!currentUser.HangarId.HasValue && !currentUser.ShopId.HasValue)
            return BadRequest("Team Leader must be assigned to a Hangar or Shop to create schedules");

        // Validate entries
        var validationErrors = new List<string>();
        var seenEntries = new HashSet<string>();

        // Batch-fetch all referenced employees in a single query
        var entryEmployeeIds = request.entries
            .Select(e => Guid.TryParse(e.employeeId, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToList();

        var employeeMap = await dbContext.Users
            .AsNoTracking()
            .Where(x => entryEmployeeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var entry in request.entries)
        {
            // Check for duplicate employee/date
            var key = $"{entry.employeeId}-{entry.date}";
            if (!seenEntries.Add(key))
                validationErrors.Add($"Duplicate entry for employee {entry.employeeId} on {entry.date}");

            // Validate shift type
            if (!ValidShiftTypes.Contains(entry.shiftType))
                validationErrors.Add($"Invalid shift type '{entry.shiftType}' for employee {entry.employeeId}");

            // Validate employee exists and belongs to same hangar/shop/section
            if (!Guid.TryParse(entry.employeeId, out var empGuid) || !employeeMap.TryGetValue(empGuid, out var employee))
            {
                validationErrors.Add($"Employee {entry.employeeId} not found");
            }
            else
            {
                // Primary validation: employee must report to this Team Leader
                if (employee.ReportsToUserId.HasValue && employee.ReportsToUserId != userId)
                {
                    validationErrors.Add($"Employee {entry.employeeId} does not report to this Team Leader");
                }
                // Fallback validation: if no ReportsToUserId, check hangar/shop/section
                else if (!employee.ReportsToUserId.HasValue)
                {
                    if (employee.SectionId != currentUser.SectionId)
                    {
                        validationErrors.Add($"Employee {entry.employeeId} is not in the same section as Team Leader");
                    }
                    else if (currentUser.HangarId.HasValue && employee.HangarId != currentUser.HangarId)
                    {
                        validationErrors.Add($"Employee {entry.employeeId} is not in the same hangar as Team Leader");
                    }
                    else if (currentUser.ShopId.HasValue && employee.ShopId != currentUser.ShopId)
                    {
                        validationErrors.Add($"Employee {entry.employeeId} is not in the same shop as Team Leader");
                    }
                }
            }
        }

        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        // Use transaction for atomic operations
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Create or update monthly schedule record
        var allSchedules = await dbContext.OperationalRecords
            .Where(x => 
                x.Module == "schedule" && 
                x.Resource == "monthly" &&
                x.CreatedBy == userId &&
                x.PayloadJson != null)
            .ToListAsync(cancellationToken);

        OperationalRecord? existingSchedule = null;
        foreach (var schedule in allSchedules)
        {
            try
            {
                var doc = JsonDocument.Parse(schedule.PayloadJson);
                if (doc.RootElement.TryGetProperty("year", out var year) && year.GetInt32() == request.year &&
                    doc.RootElement.TryGetProperty("month", out var month) && month.GetInt32() == request.month)
                {
                    existingSchedule = schedule;
                    break;
                }
            }
            catch { }
        }

        var payload = new
        {
            request.year,
            request.month,
            request.status,
            sectionId = currentUser.SectionId,
            hangarId = currentUser.HangarId,
            shopId = currentUser.ShopId,
            teamLeaderId = userId,
            entries = request.entries.Select(e => new
            {
                e.employeeId,
                e.employeeName,
                e.date,
                e.shiftType,
                e.isAceActivity
            })
        };

        if (existingSchedule is not null)
        {
            // Concurrency check: verify the record hasn't been modified since last read
            if (!string.IsNullOrEmpty(request.version))
            {
                var expectedVersion = uint.Parse(request.version);
                if (existingSchedule.Version != expectedVersion)
                {
                    return Conflict(new { 
                        error = "Concurrency conflict", 
                        message = "This schedule has been modified by another user. Please refresh and try again.",
                        currentVersion = existingSchedule.Version
                    });
                }
            }

            existingSchedule.PayloadJson = JsonSerializer.Serialize(payload);
            existingSchedule.Status = request.status;
            existingSchedule.UpdatedAt = DateTimeOffset.UtcNow;
            // Version will be auto-incremented by EF Core due to IsConcurrencyToken()
        }
        else
        {
            var schedule = new OperationalRecord
            {
                Module = "schedule",
                Resource = "monthly",
                Action = "save",
                PayloadJson = JsonSerializer.Serialize(payload),
                Status = request.status,
                CreatedBy = userId
            };
            dbContext.OperationalRecords.Add(schedule);
        }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        
        // Verify the save by querying it back immediately
        var verifyQuery = dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "schedule" && x.Resource == "monthly" && x.CreatedBy == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefault();
        
        if (verifyQuery != null)
        {
            Console.WriteLine($"[ScheduleController] ✓ VERIFIED: Schedule saved to DB. RecordId={verifyQuery.Id}, UpdatedAt={verifyQuery.UpdatedAt}");
        }
        else
        {
            Console.WriteLine($"[ScheduleController] ✗ ERROR: Schedule NOT found in DB after save!");
        }
        
        return Ok(new { success = true, message = "Schedule saved successfully", recordId = verifyQuery?.Id, version = verifyQuery?.Version });
    }

    [HttpGet("monthly-schedules")]
    public async Task<IActionResult> GetMonthlySchedule([FromQuery] int year, [FromQuery] int month, [FromQuery] Guid? sectionId, [FromQuery] Guid? hangarId, [FromQuery] Guid? shopId, [FromQuery] Guid? teamLeaderId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
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

        Console.WriteLine($"[ScheduleController] GetMonthlySchedule: userId={userId}, role={currentUser.Role}, year={year}, month={month}");

        var query = dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "schedule" && x.Resource == "monthly");

        // Apply RBAC filters
        if (currentUser.Role == UserRole.Employee)
        {
            // NEW: Check if employee has a Team Leader assigned via ReportsToUserId
            if (currentUser.ReportsToUserId.HasValue)
            {
                // Employee sees only their assigned Team Leader's schedules
                query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == currentUser.ReportsToUserId.Value);
            }
            else
            {
                // Fallback: Employees see schedules from any Team Leader in their hangar/shop
                var teamLeaderIds = await dbContext.Users
                    .AsNoTracking()
                    .Where(x => x.Role == UserRole.TeamLeader &&
                                ((currentUser.HangarId.HasValue && x.HangarId == currentUser.HangarId) ||
                                 (currentUser.ShopId.HasValue && x.ShopId == currentUser.ShopId)))
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (teamLeaderIds.Count > 0)
                {
                    query = query.Where(x => x.CreatedBy.HasValue && teamLeaderIds.Contains(x.CreatedBy.Value));
                }
                else
                {
                    // No team leader found, return empty
                    query = query.Where(x => false);
                }
            }
        }
        else if (currentUser.Role == UserRole.TeamLeader)
        {
            Console.WriteLine($"[ScheduleController] Team Leader branch: userId={userId}, hangarId={currentUser.HangarId}, shopId={currentUser.ShopId}");

            // If teamLeaderId is provided, fetch that specific team leader's schedule (must be in same hangar or shop)
            if (teamLeaderId.HasValue)
            {
                Console.WriteLine($"[ScheduleController] Filtering by specific teamLeaderId={teamLeaderId.Value}");
                // Verify the requested team leader is in the same hangar or shop
                var requestedTeamLeader = await dbContext.Users
                    .AsNoTracking()
                    .Where(x => x.Id == teamLeaderId.Value && x.Role == UserRole.TeamLeader &&
                                ((currentUser.HangarId.HasValue && x.HangarId == currentUser.HangarId) ||
                                 (currentUser.ShopId.HasValue && x.ShopId == currentUser.ShopId)))
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (requestedTeamLeader is null)
                {
                    // Requested team leader not in same hangar/shop - return empty
                    query = query.Where(x => false);
                }
                else
                {
                    // Fetch only that team leader's schedule
                    query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == teamLeaderId.Value);
                }
            }
            else
            {
                Console.WriteLine($"[ScheduleController] No teamLeaderId filter, fetching own schedules");
                // Always include the team leader's own schedule (CreatedBy == userId)
                query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == userId);

            }
        }
        else if (currentUser.Role == UserRole.Manager)
        {
            // Managers see all schedules in their Section, with optional hangar/shop filtering
            if (currentUser.SectionId.HasValue)
            {
                var sectionUserIds = dbContext.Users
                    .Where(x => x.SectionId == currentUser.SectionId)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && sectionUserIds.Contains(x.CreatedBy.Value));
                
                // Additional filter by hangarId if provided (must be within manager's section)
                if (hangarId.HasValue)
                {
                    var hangarUserIds = dbContext.Users
                        .Where(x => x.HangarId == hangarId.Value && x.SectionId == currentUser.SectionId)
                        .Select(x => x.Id)
                        .ToList();
                    query = query.Where(x => x.CreatedBy.HasValue && hangarUserIds.Contains(x.CreatedBy.Value));
                }
            }
        }
        else if (currentUser.Role == UserRole.Director || currentUser.Role == UserRole.SystemAdmin)
        {
            // Directors and Admins see all schedules, with optional section/hangar/shop filtering
            if (sectionId.HasValue)
            {
                var sectionUserIds = dbContext.Users
                    .Where(x => x.SectionId == sectionId.Value)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && sectionUserIds.Contains(x.CreatedBy.Value));
            }
            if (hangarId.HasValue)
            {
                var hangarUserIds = dbContext.Users
                    .Where(x => x.HangarId == hangarId.Value)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && hangarUserIds.Contains(x.CreatedBy.Value));
            }
            if (shopId.HasValue)
            {
                var shopUserIds = dbContext.Users
                    .Where(x => x.ShopId == shopId.Value)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && shopUserIds.Contains(x.CreatedBy.Value));
            }
            if (teamLeaderId.HasValue)
            {
                query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == teamLeaderId.Value);
            }
        }

        // Log the query for debugging
        var recordCount = await query.CountAsync(cancellationToken);
        Console.WriteLine($"[ScheduleController] Query found {recordCount} OperationalRecords for module=schedule, resource=monthly");

        // Apply pagination
        var skip = (page - 1) * pageSize;
        var records = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        // For Team Leaders, get their direct reports for filtering entries
        // For Employees, they should only see their own entries
        HashSet<Guid>? visibleEmployeeIds = null;
        if (currentUser.Role == UserRole.TeamLeader)
        {
            visibleEmployeeIds = new HashSet<Guid>(
                await dbContext.Users
                    .AsNoTracking()
                    .Where(x => x.Role == UserRole.Employee && x.ReportsToUserId == userId)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken)
            );
            Console.WriteLine($"[ScheduleController] Team Leader {currentUser.FullName} ({userId}) has {visibleEmployeeIds.Count} direct reports");
        }
        else if (currentUser.Role == UserRole.Employee)
        {
            // Employees only see their own entries
            visibleEmployeeIds = new HashSet<Guid> { userId.Value };
            Console.WriteLine($"[ScheduleController] Employee {currentUser.FullName} ({userId}) can only see their own entries");
        }

        var schedules = records
            .Where(r => r.PayloadJson != null)
            .Select(r =>
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<JsonElement>(r.PayloadJson);
                    var entries = payload.TryGetProperty("entries", out var entriesProp) && entriesProp.ValueKind == JsonValueKind.Array
                        ? entriesProp.EnumerateArray()
                            .Select(e => new
                            {
                                employeeId = e.TryGetProperty("employeeId", out var eid) ? eid.GetString() : "",
                                employeeName = e.TryGetProperty("employeeName", out var ename) ? ename.GetString() : "",
                                date = e.TryGetProperty("date", out var date) ? date.GetString() : "",
                                shiftType = e.TryGetProperty("shiftType", out var shift) ? shift.GetString() : "Day",
                                isAceActivity = e.TryGetProperty("isAceActivity", out var ace) ? ace.GetBoolean() : false
                            })
                            .Where(e =>
                            {
                                // For Team Leaders and Employees, filter entries to only show visible employees
                                if (visibleEmployeeIds != null)
                                {
                                    if (Guid.TryParse(e.employeeId, out var empGuid))
                                    {
                                        var isVisible = visibleEmployeeIds.Contains(empGuid);
                                        if (!isVisible)
                                        {
                                            Console.WriteLine($"[ScheduleController] Filtering out employee {e.employeeName} ({e.employeeId}) - not visible to {currentUser.Role} {currentUser.FullName}");
                                        }
                                        return isVisible;
                                    }
                                    Console.WriteLine($"[ScheduleController] Could not parse employeeId: {e.employeeId}");
                                    return false;
                                }
                                return true;
                            })
                            .ToArray()
                        : Array.Empty<object>();

                    return new
                    {
                        id = r.Id,
                        year = payload.TryGetProperty("year", out var y) ? y.GetInt32() : 0,
                        month = payload.TryGetProperty("month", out var m) ? m.GetInt32() : 0,
                        status = r.Status,
                        sectionId = payload.TryGetProperty("sectionId", out var sid) && sid.ValueKind != JsonValueKind.Null ? (Guid?)sid.GetGuid() : null,
                        hangarId = payload.TryGetProperty("hangarId", out var hid) && hid.ValueKind != JsonValueKind.Null ? (Guid?)hid.GetGuid() : null,
                        teamLeaderId = payload.TryGetProperty("teamLeaderId", out var tlid) && tlid.ValueKind != JsonValueKind.Null ? (Guid?)tlid.GetGuid() : null,
                        entries = entries
                    };
                }
                catch
                {
                    return null;
                }
            })
            .Where(x => x is not null)
            .Where(x => x is not null && x.year == year && x.month == month)
            .ToList();

        // Log for debugging
#pragma warning disable CS8602
        Console.WriteLine($"[ScheduleController] Get schedule for year={year}, month={month}, userId={userId}, role={currentUser.Role}, found={schedules.Count}, entryCount={schedules.Sum(s => s.entries?.Length ?? 0)}");

        // ==================== LEAVE OVERLAY: Inject vacation entries from finalized leave plans ====================
        var monthStart = new DateTimeOffset(year, month + 1, 1, 0, 0, 0, TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        // Build leave entry lookup by employeeId+date for O(1) replacement
        var leaveEntries = await dbContext.AnnualLeavePlanEntries
            .Include(e => e.AnnualLeavePlan)
            .Include(e => e.User)
            .AsNoTracking()
            .Where(e => e.AnnualLeavePlan.Status == AnnualLeavePlanStatus.Finalized
                && e.AnnualLeavePlan.Year == year
                && e.ApprovedStartDate <= monthEnd
                && e.ApprovedEndDate >= monthStart)
            .ToListAsync(cancellationToken);

        // Apply RBAC scope filtering to leave entries
        leaveEntries = leaveEntries.Where(e =>
        {
            var plan = e.AnnualLeavePlan;
            // Manager: same section
            if (currentUser.Role == UserRole.Manager && currentUser.SectionId.HasValue)
                return plan.SectionId == currentUser.SectionId.Value;
            // Director: apply filters if provided
            if (currentUser.Role == UserRole.Director || currentUser.Role == UserRole.SystemAdmin)
            {
                if (sectionId.HasValue && plan.SectionId != sectionId.Value) return false;
                if (hangarId.HasValue && plan.HangarId != hangarId.Value) return false;
                if (shopId.HasValue && plan.ShopId != shopId.Value) return false;
                if (teamLeaderId.HasValue && plan.TeamLeaderId != teamLeaderId.Value) return false;
                return true;
            }
            // Team Leader: only show leave for employees that report to them
            if (currentUser.Role == UserRole.TeamLeader)
            {
                if (visibleEmployeeIds != null)
                {
                    return visibleEmployeeIds.Contains(e.User.Id);
                }
                // Fallback to hangar/shop if no direct reports
                if (currentUser.HangarId.HasValue && plan.HangarId != currentUser.HangarId.Value) return false;
                if (currentUser.ShopId.HasValue && plan.ShopId != currentUser.ShopId.Value) return false;
                return true;
            }
            // Employee: only show their own leave
            if (currentUser.Role == UserRole.Employee)
            {
                return e.User.Id == userId.Value;
            }
            return true;
        }).ToList();

        // Inject vacation entries into schedule entries
        var entriesToReturn = schedules.FirstOrDefault()?.entries?.ToList() ?? new List<object>();
        foreach (var leaveEntry in leaveEntries)
        {
            var leaveStart = leaveEntry.ApprovedStartDate.Date;
            var leaveEnd = leaveEntry.ApprovedEndDate.Date;
            var current = leaveStart;
            while (current <= leaveEnd)
            {
                if (current.Year == year && current.Month == month + 1)
                {
                    var dateStr = current.ToString("yyyy-MM-dd");
                    var empId = leaveEntry.User.Id.ToString();
                    var empName = leaveEntry.User.FullName ?? "Unknown";

                    // Find and replace existing entry, or add new one
                    var existingIdx = entriesToReturn.FindIndex(e =>
                    {
                        if (e is not JsonElement) return false;
                        var elem = (JsonElement)e;
                        return elem.TryGetProperty("employeeId", out var eid) && eid.GetString() == empId
                            && elem.TryGetProperty("date", out var d) && d.GetString() == dateStr;
                    });

                    var vacationEntry = new
                    {
                        employeeId = empId,
                        employeeName = empName,
                        date = dateStr,
                        shiftType = "Vacation",
                        isAceActivity = false,
                        isLeaveOverlay = true // Flag to indicate this is from leave plan
                    };

                    if (existingIdx >= 0)
                    {
                        entriesToReturn[existingIdx] = vacationEntry;
                    }
                    else
                    {
                        entriesToReturn.Add(vacationEntry);
                    }
                }
                current = current.AddDays(1);
            }
        }

        Console.WriteLine($"[ScheduleController] Leave overlay: injected {leaveEntries.Count} leave entries, total entries after overlay: {entriesToReturn.Count}");

        // If Team Leader is querying for workspace view (no teamLeaderId but hangarId or shopId provided), return all schedules
        // Otherwise return single schedule (original behavior)
        if (currentUser.Role == UserRole.TeamLeader && !teamLeaderId.HasValue && (hangarId.HasValue || shopId.HasValue))
        {
            // Return all schedules combined for workspace view with leave overlay applied
            var allEntries = schedules.Where(s => s.entries != null).SelectMany(s => s.entries!).ToList();
            // Apply leave overlay to workspace entries as well
            foreach (var leaveEntry in leaveEntries)
            {
                var leaveStart = leaveEntry.ApprovedStartDate.Date;
                var leaveEnd = leaveEntry.ApprovedEndDate.Date;
                var current = leaveStart;
                while (current <= leaveEnd)
                {
                    if (current.Year == year && current.Month == month + 1)
                    {
                        var dateStr = current.ToString("yyyy-MM-dd");
                        var empId = leaveEntry.User.Id.ToString();
                        var empName = leaveEntry.User.FullName ?? "Unknown";

                        var existingIdx = allEntries.FindIndex(e =>
                        {
                            if (e is not JsonElement) return false;
                            var elem = (JsonElement)e;
                            return elem.TryGetProperty("employeeId", out var eid) && eid.GetString() == empId
                                && elem.TryGetProperty("date", out var d) && d.GetString() == dateStr;
                        });

                        var vacationEntry = new
                        {
                            employeeId = empId,
                            employeeName = empName,
                            date = dateStr,
                            shiftType = "Vacation",
                            isAceActivity = false,
                            isLeaveOverlay = true
                        };

                        if (existingIdx >= 0)
                        {
                            allEntries[existingIdx] = vacationEntry;
                        }
                        else
                        {
                            allEntries.Add(vacationEntry);
                        }
                    }
                    current = current.AddDays(1);
                }
            }
#pragma warning restore CS8602
            var workspaceType = hangarId.HasValue ? "hangar" : "shop";
            Console.WriteLine($"[ScheduleController] Returning {schedules.Count} schedules for {workspaceType} view with {allEntries.Count} total entries (including leave overlay)");
            return Ok(new
            {
                schedules = schedules,
                entries = allEntries.ToArray(),
                pagination = new
                {
                    page,
                    pageSize,
                    totalRecords = recordCount,
                    totalPages = (int)Math.Ceiling((double)recordCount / pageSize)
                }
            });
        }

        return Ok(new
        {
            schedule = schedules.FirstOrDefault(),
            entries = entriesToReturn.ToArray(),
            version = schedules.FirstOrDefault()?.id != null ? records.FirstOrDefault(r => r.Id == schedules.FirstOrDefault()!.id)?.Version : null,
            pagination = new
            {
                page,
                pageSize,
                totalRecords = recordCount,
                totalPages = (int)Math.Ceiling((double)recordCount / pageSize)
            }
        });
    }

    public sealed record SaveScheduleRequest(int year, int month, string status, ScheduleEntry[] entries, string? version = null);
    public sealed record ScheduleEntry(string employeeId, string employeeName, string date, string shiftType, bool isAceActivity);

    // ==================== DAILY ASSIGNMENT ENDPOINTS ====================

    [HttpPost("dailyassignment")]
    public async Task<IActionResult> SaveDailyAssignment([FromBody] SaveDailyAssignmentRequest request, CancellationToken cancellationToken)
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
        
        // Only Team Leaders can create daily assignments
        if (currentUser.Role != UserRole.TeamLeader)
            return Forbid("Only Team Leaders can create daily assignments");

        // Validate user has hangar or shop assignment
        if (!currentUser.HangarId.HasValue && !currentUser.ShopId.HasValue)
            return BadRequest("Team Leader must be assigned to a Hangar or Shop to create daily assignments");

        // Validate entries
        var validationErrors = new List<string>();

        // CRITICAL: Shop Team Leaders cannot create aircraft assignments
        if (currentUser.ShopId.HasValue && !currentUser.HangarId.HasValue)
        {
            if (!string.IsNullOrEmpty(request.aircraftType) || 
                !string.IsNullOrEmpty(request.aircraftRegistration) ||
                (request.aircrafts != null && request.aircrafts.Length > 0))
            {
                validationErrors.Add("Shop Team Leaders cannot create aircraft assignments. Aircraft data is not allowed for shop-based assignments.");
            }
        }

        // Batch-fetch all referenced employees
        var employeeIds = request.details
            .Select(d => Guid.TryParse(d.employeeId, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToList();

        var employeeMap = await dbContext.Users
            .AsNoTracking()
            .Where(x => employeeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        // Validate employee count matches expected manpower
        if (request.details.Length != request.expectedManpower)
            validationErrors.Add($"Assigned employees ({request.details.Length}) must match Expected Manpower ({request.expectedManpower})");

        foreach (var detail in request.details)
        {
            // Validate employee exists and belongs to same hangar/shop/section
            if (!Guid.TryParse(detail.employeeId, out var empGuid) || !employeeMap.TryGetValue(empGuid, out var employee))
            {
                validationErrors.Add($"Employee {detail.employeeId} not found");
            }
            else if (employee.SectionId != currentUser.SectionId)
            {
                validationErrors.Add($"Employee {detail.employeeId} is not in the same section as Team Leader");
            }
            else if (currentUser.HangarId.HasValue && employee.HangarId != currentUser.HangarId)
            {
                validationErrors.Add($"Employee {detail.employeeId} is not in the same hangar as Team Leader");
            }
            else if (currentUser.ShopId.HasValue && employee.ShopId != currentUser.ShopId)
            {
                validationErrors.Add($"Employee {detail.employeeId} is not in the same shop as Team Leader");
            }
        }

        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        // Use transaction for atomic operations
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Create daily assignment record
        var payload = new
        {
            request.date,
            request.shift,
            request.aircraftType,
            request.aircraftRegistration,
            request.expectedManpower,
            request.aircrafts,
            request.status,
            sectionId = currentUser.SectionId,
            hangarId = currentUser.HangarId,
            shopId = currentUser.ShopId,
            teamLeaderId = userId,
            details = request.details.Select(d => new
            {
                d.employeeId,
                d.employeeName,
                d.position,
                d.assignedAircraft,
                d.taskDescription
            })
        };

        var assignment = new OperationalRecord
        {
            Module = "dailyassignment",
            Resource = "assignment",
            Action = "create",
            PayloadJson = JsonSerializer.Serialize(payload),
            Status = request.status,
            CreatedBy = userId
        };
        dbContext.OperationalRecords.Add(assignment);

            await dbContext.SaveChangesAsync(cancellationToken);
            var assignmentId = assignment.Id;
            await transaction.CommitAsync(cancellationToken);
            return Ok(new { success = true, message = "Daily assignment saved successfully", id = assignmentId });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    [HttpGet("dailyassignment")]
    public async Task<IActionResult> GetDailyAssignments([FromQuery] string date, [FromQuery] Guid? sectionId, [FromQuery] Guid? workspaceId, [FromQuery] Guid? teamLeaderId, [FromQuery] Guid? employeeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
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

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;

        var query = dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "dailyassignment" && x.Resource == "assignment");

        List<Guid>? teamLeaderEmployeeIdsInScope = null;

        // Apply RBAC filters
        if (currentUser.Role == UserRole.Employee)
        {
            // NEW: Check if employee has a Team Leader assigned via ReportsToUserId
            if (currentUser.ReportsToUserId.HasValue)
            {
                // Employee sees only their assigned Team Leader's assignments
                query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == currentUser.ReportsToUserId.Value);
            }
            else
            {
                // Fallback: Employees see assignments where they are assigned
                query = query.Where(x => x.PayloadJson != null && 
                    x.PayloadJson.Contains($"\"employeeId\":\"{userId}\""));
            }
        }
        else if (currentUser.Role == UserRole.TeamLeader)
        {
            // NEW: Check employees' ReportsToUserId first for proper team isolation
            teamLeaderEmployeeIdsInScope = await dbContext.Users
                .AsNoTracking()
                .Where(x => x.ReportsToUserId == userId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            // Fallback: If no employees have ReportsToUserId set, use workspace-based filtering
            if (!teamLeaderEmployeeIdsInScope.Any())
            {
                var workspaceEmployeeIds = await dbContext.Users
                    .AsNoTracking()
                    .Where(x => (currentUser.HangarId.HasValue && x.HangarId == currentUser.HangarId) ||
                               (currentUser.ShopId.HasValue && x.ShopId == currentUser.ShopId))
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);
                teamLeaderEmployeeIdsInScope = workspaceEmployeeIds;
            }

            // Always include the Team Leader's own assignments
            if (!teamLeaderEmployeeIdsInScope.Contains(userId.Value))
            {
                teamLeaderEmployeeIdsInScope.Add(userId.Value);
            }

            query = query.Where(x => x.PayloadJson != null);
        }
        else if (currentUser.Role == UserRole.Manager)
        {
            // Managers see assignments in their section
            if (currentUser.SectionId.HasValue)
            {
                query = query.Where(x => x.PayloadJson != null && 
                    x.PayloadJson.Contains($"\"sectionId\":\"{currentUser.SectionId}\""));
            }
        }
        // Directors and Admins see all (no filter)

        // Apply additional filters
        if (!string.IsNullOrEmpty(date))
        {
            query = query.Where(x => x.PayloadJson != null && 
                x.PayloadJson.Contains($"\"date\":\"{date}\""));
        }
        if (sectionId.HasValue)
        {
            query = query.Where(x => x.PayloadJson != null && 
                x.PayloadJson.Contains($"\"sectionId\":\"{sectionId}\""));
        }
        if (workspaceId.HasValue)
        {
            query = query.Where(x => x.PayloadJson != null && 
                (x.PayloadJson.Contains($"\"hangarId\":\"{workspaceId}\"") ||
                 x.PayloadJson.Contains($"\"shopId\":\"{workspaceId}\"")));
        }
        if (teamLeaderId.HasValue)
        {
            query = query.Where(x => x.CreatedBy == teamLeaderId.Value);
        }

        var records = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

        if (currentUser.Role == UserRole.TeamLeader && teamLeaderEmployeeIdsInScope is not null)
        {
            records = records.Where(x =>
                (x.CreatedBy.HasValue && x.CreatedBy.Value == userId.Value) ||
                (x.PayloadJson != null && teamLeaderEmployeeIdsInScope.Any(eid => x.PayloadJson.Contains($"\"employeeId\":\"{eid}\"")))
            ).ToList();
        }

        var assignments = new List<object>();
        var details = new List<object>();

        foreach (var record in records)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
                var assignmentObj = new
                {
                    id = record.Id,
                    date = payload.TryGetProperty("date", out var d) ? d.GetString() : "",
                    aircraftType = payload.TryGetProperty("aircraftType", out var at) ? at.GetString() : "",
                    aircraftRegistration = payload.TryGetProperty("aircraftRegistration", out var ar) ? ar.GetString() : "",
                    expectedManpower = payload.TryGetProperty("expectedManpower", out var em) ? em.GetInt32() : 0,
                    status = record.Status,
                    sectionId = payload.TryGetProperty("sectionId", out var sid) && sid.ValueKind != JsonValueKind.Null ? (Guid?)sid.GetGuid() : null,
                    hangarId = payload.TryGetProperty("hangarId", out var hid) && hid.ValueKind != JsonValueKind.Null ? (Guid?)hid.GetGuid() : null,
                    teamLeaderId = payload.TryGetProperty("teamLeaderId", out var tlid) && tlid.ValueKind != JsonValueKind.Null ? (Guid?)tlid.GetGuid() : null,
                    createdAt = record.CreatedAt
                };
                assignments.Add(assignmentObj);

                if (payload.TryGetProperty("details", out var detailsProp) && detailsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var detail in detailsProp.EnumerateArray())
                    {
                        details.Add(new
                        {
                            id = $"{record.Id}-{(detail.TryGetProperty("employeeId", out var eid) ? eid.GetString() : "")}",
                            dailyAssignmentId = record.Id,
                            employeeId = detail.TryGetProperty("employeeId", out var eid2) ? eid2.GetString() : "",
                            employeeName = detail.TryGetProperty("employeeName", out var ename) ? ename.GetString() : "",
                            position = detail.TryGetProperty("position", out var pos) ? pos.GetString() : "",
                            assignedAircraft = detail.TryGetProperty("assignedAircraft", out var aa) ? aa.GetString() : "",
                            taskDescription = detail.TryGetProperty("taskDescription", out var td) ? td.GetString() : ""
                        });
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return Ok(new { assignments, details });
    }

    [HttpGet("dailyassignment/{id}")]
    public async Task<IActionResult> GetDailyAssignment(Guid id, CancellationToken cancellationToken = default)
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
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "dailyassignment" && x.Resource == "assignment", cancellationToken);
        
        if (record is null) return NotFound();

        // NEW: Parse payload to extract organizational context for RBAC
        JsonElement? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
        }
        catch
        {
            return BadRequest("Failed to parse assignment data");
        }

        Guid? recordSectionId = null;
        Guid? recordHangarId = null;
        Guid? recordShopId = null;
        Guid? recordTeamLeaderId = null;

        if (payload.HasValue)
        {
            if (payload.Value.TryGetProperty("sectionId", out var sid) && sid.ValueKind != JsonValueKind.Null)
                recordSectionId = sid.GetGuid();
            if (payload.Value.TryGetProperty("hangarId", out var hid) && hid.ValueKind != JsonValueKind.Null)
                recordHangarId = hid.GetGuid();
            if (payload.Value.TryGetProperty("shopId", out var shid) && shid.ValueKind != JsonValueKind.Null)
                recordShopId = shid.GetGuid();
            if (payload.Value.TryGetProperty("teamLeaderId", out var tlid) && tlid.ValueKind != JsonValueKind.Null)
                recordTeamLeaderId = tlid.GetGuid();
        }

        // Apply RBAC based on role
        bool hasAccess = currentUser.Role switch
        {
            UserRole.Employee => recordTeamLeaderId == userId || 
                                (currentUser.ReportsToUserId.HasValue && recordTeamLeaderId == currentUser.ReportsToUserId.Value) ||
                                (currentUser.HangarId.HasValue && recordHangarId == currentUser.HangarId) ||
                                (currentUser.ShopId.HasValue && recordShopId == currentUser.ShopId),
            UserRole.TeamLeader => recordTeamLeaderId == userId ||
                                (currentUser.HangarId.HasValue && recordHangarId == currentUser.HangarId) ||
                                (currentUser.ShopId.HasValue && recordShopId == currentUser.ShopId),
            UserRole.Manager => currentUser.SectionId.HasValue && recordSectionId == currentUser.SectionId,
            UserRole.Director or UserRole.SystemAdmin => true,
            _ => false
        };

        if (!hasAccess)
            return Forbid("You do not have access to this assignment");

        try
        {
            var date = payload.HasValue && payload.Value.TryGetProperty("date", out var d) ? d.GetString() : "";
            var aircraftType = payload.HasValue && payload.Value.TryGetProperty("aircraftType", out var at) ? at.GetString() : "";
            var aircraftRegistration = payload.HasValue && payload.Value.TryGetProperty("aircraftRegistration", out var ar) ? ar.GetString() : "";
            var expectedManpower = payload.HasValue && payload.Value.TryGetProperty("expectedManpower", out var em) ? em.GetInt32() : 0;

            var assignment = new
            {
                id = record.Id,
                date,
                aircraftType,
                aircraftRegistration,
                expectedManpower,
                status = record.Status,
                sectionId = recordSectionId,
                hangarId = recordHangarId,
                teamLeaderId = recordTeamLeaderId,
                createdAt = record.CreatedAt
            };

            var details = new List<object>();
            if (payload.HasValue && payload.Value.TryGetProperty("details", out var detailsProp) && detailsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var detail in detailsProp.EnumerateArray())
                {
                    details.Add(new
                    {
                        id = $"{record.Id}-{(detail.TryGetProperty("employeeId", out var eid) ? eid.GetString() : "")}",
                        dailyAssignmentId = record.Id,
                        employeeId = detail.TryGetProperty("employeeId", out var eid2) ? eid2.GetString() : "",
                        employeeName = detail.TryGetProperty("employeeName", out var ename) ? ename.GetString() : "",
                        position = detail.TryGetProperty("position", out var pos) ? pos.GetString() : "",
                        assignedAircraft = detail.TryGetProperty("assignedAircraft", out var aa) ? aa.GetString() : "",
                        taskDescription = detail.TryGetProperty("taskDescription", out var td) ? td.GetString() : ""
                    });
                }
            }

            return Ok(new { assignment, details });
        }
        catch
        {
            return BadRequest("Failed to parse assignment data");
        }
    }

    [HttpPost("dailyassignment/{id}/publish")]
    public async Task<IActionResult> PublishDailyAssignment(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        
        if (currentUser is null) return Unauthorized();
        
        if (currentUser.Role != UserRole.TeamLeader)
            return Forbid("Only Team Leaders can publish daily assignments");

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "dailyassignment" && x.Resource == "assignment", cancellationToken);
        
        if (record is null) return NotFound();

        // Verify the team leader created this assignment
        if (record.CreatedBy != userId)
            return Forbid("You can only publish assignments you created");

        record.Status = "Published";
        record.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        
        return Ok(new { success = true, message = "Daily assignment published successfully" });
    }

    public sealed record SaveDailyAssignmentRequest(
        string date,
        string shift,
        string aircraftType,
        string aircraftRegistration,
        int expectedManpower,
        object[] aircrafts,
        string status,
        DailyAssignmentDetail[] details
    );

    public sealed record DailyAssignmentDetail(
        string employeeId,
        string employeeName,
        string position,
        string assignedAircraft,
        string taskDescription
    );

    // ==================== ALL SCHEDULES ENDPOINT FOR MANPOWER CALCULATION ====================

    [HttpGet("monthly-schedules/all")]
    public async Task<IActionResult> GetAllMonthlySchedules([FromQuery] Guid? sectionId, [FromQuery] Guid? hangarId, [FromQuery] Guid? shopId, [FromQuery] Guid? teamLeaderId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
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
            .Where(x => x.Module == "schedule" && x.Resource == "monthly");

        // Apply RBAC filters (same logic as GetMonthlySchedule but without year/month filter)
        if (currentUser.Role == UserRole.Employee)
        {
            // NEW: Check if employee has a Team Leader assigned via ReportsToUserId
            if (currentUser.ReportsToUserId.HasValue)
            {
                // Employee sees only their assigned Team Leader's schedules
                query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == currentUser.ReportsToUserId.Value);
            }
            else
            {
                // Fallback: Employees see schedules from any Team Leader in their hangar/shop
                var teamLeaderIds = await dbContext.Users
                    .AsNoTracking()
                    .Where(x => x.Role == UserRole.TeamLeader &&
                                ((currentUser.HangarId.HasValue && x.HangarId == currentUser.HangarId) ||
                                 (currentUser.ShopId.HasValue && x.ShopId == currentUser.ShopId)))
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (teamLeaderIds.Count > 0)
                {
                    query = query.Where(x => x.CreatedBy.HasValue && teamLeaderIds.Contains(x.CreatedBy.Value));
                }
                else
                {
                    query = query.Where(x => false);
                }
            }
        }
        else if (currentUser.Role == UserRole.TeamLeader)
        {
            if (teamLeaderId.HasValue)
            {
                var requestedTeamLeader = await dbContext.Users
                    .AsNoTracking()
                    .Where(x => x.Id == teamLeaderId.Value && x.Role == UserRole.TeamLeader &&
                                ((currentUser.HangarId.HasValue && x.HangarId == currentUser.HangarId) ||
                                 (currentUser.ShopId.HasValue && x.ShopId == currentUser.ShopId)))
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (requestedTeamLeader is null)
                {
                    query = query.Where(x => false);
                }
                else
                {
                    query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == teamLeaderId.Value);
                }
            }
            else
            {
                // Always include the team leader's own schedule (CreatedBy == userId)
                query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == userId);

                // NEW: Check employees' ReportsToUserId first for proper team isolation
                var employeeIdsInScope = await dbContext.Users
                    .AsNoTracking()
                    .Where(x => x.ReportsToUserId == userId)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (employeeIdsInScope.Any())
                {
                    // Filter schedules where entries contain employees reporting to this TL
                    // Note: PayloadJson filtering must be done in-memory since EF Core can't translate JSON queries
                    var earlyFilteredRecords = await query.ToListAsync(cancellationToken);
                    earlyFilteredRecords = earlyFilteredRecords.Where(x => x.PayloadJson != null &&
                        employeeIdsInScope.Any(eid => x.PayloadJson.Contains($"\"employeeId\":\"{eid}\""))).ToList();

                    var earlyRecordCount = earlyFilteredRecords.Count;
                    Console.WriteLine($"[ScheduleController] Query found {earlyRecordCount} OperationalRecords for module=schedule, resource=monthly");

                    var earlySkip = (page - 1) * pageSize;
                    var earlyPaginatedRecords = earlyFilteredRecords.Skip(earlySkip).Take(pageSize).ToList();

                    var earlySchedules = earlyPaginatedRecords
                        .Where(r => r.PayloadJson != null)
                        .Select(r =>
                        {
                            try
                            {
                                var payload = JsonSerializer.Deserialize<JsonElement>(r.PayloadJson);
                                return new
                                {
                                    id = r.Id,
                                    year = payload.TryGetProperty("year", out var y) ? y.GetInt32() : 0,
                                    month = payload.TryGetProperty("month", out var m) ? m.GetInt32() : 0,
                                    status = r.Status,
                                    createdAt = r.CreatedAt,
                                    updatedAt = r.UpdatedAt,
                                    createdBy = r.CreatedBy,
                                    payload = r.PayloadJson
                                };
                            }
                            catch
                            {
                                return null;
                            }
                        })
                        .Where(x => x != null)
                        .ToList();

                    return Ok(ApiResults.Page<object>(earlySchedules.Cast<object>().ToList(), earlyRecordCount, page, pageSize));
                }
                else
                {
                    // Fallback: Team Leaders see their own team + ALL schedules in their Hangar or Shop
                    if (currentUser.HangarId.HasValue)
                    {
                        var hangarTeamLeaderIds = dbContext.Users
                            .Where(x => x.HangarId == currentUser.HangarId && x.Role == UserRole.TeamLeader)
                            .Select(x => x.Id)
                            .ToList();
                        query = query.Where(x => x.CreatedBy.HasValue && hangarTeamLeaderIds.Contains(x.CreatedBy.Value));
                    }
                    else if (currentUser.ShopId.HasValue)
                    {
                        var shopTeamLeaderIds = dbContext.Users
                            .Where(x => x.ShopId == currentUser.ShopId && x.Role == UserRole.TeamLeader)
                            .Select(x => x.Id)
                            .ToList();
                        query = query.Where(x => x.CreatedBy.HasValue && shopTeamLeaderIds.Contains(x.CreatedBy.Value));
                    }
                    else
                    {
                        query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == userId);
                    }
                }
            }
        }
        else if (currentUser.Role == UserRole.Manager)
        {
            if (currentUser.SectionId.HasValue)
            {
                var sectionUserIds = dbContext.Users
                    .Where(x => x.SectionId == currentUser.SectionId)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && sectionUserIds.Contains(x.CreatedBy.Value));
                
                if (hangarId.HasValue)
                {
                    var hangarUserIds = dbContext.Users
                        .Where(x => x.HangarId == hangarId.Value && x.SectionId == currentUser.SectionId)
                        .Select(x => x.Id)
                        .ToList();
                    query = query.Where(x => x.CreatedBy.HasValue && hangarUserIds.Contains(x.CreatedBy.Value));
                }
            }
        }
        else if (currentUser.Role == UserRole.Director || currentUser.Role == UserRole.SystemAdmin)
        {
            if (sectionId.HasValue)
            {
                var sectionUserIds = dbContext.Users
                    .Where(x => x.SectionId == sectionId.Value)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && sectionUserIds.Contains(x.CreatedBy.Value));
            }
            if (hangarId.HasValue)
            {
                var hangarUserIds = dbContext.Users
                    .Where(x => x.HangarId == hangarId.Value)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && hangarUserIds.Contains(x.CreatedBy.Value));
            }
            if (shopId.HasValue)
            {
                var shopUserIds = dbContext.Users
                    .Where(x => x.ShopId == shopId.Value)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && shopUserIds.Contains(x.CreatedBy.Value));
            }
            if (teamLeaderId.HasValue)
            {
                query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == teamLeaderId.Value);
            }
        }

        var records = await query.OrderByDescending(x => x.UpdatedAt).ToListAsync(cancellationToken);

        var schedules = records
            .Where(r => r.PayloadJson != null)
            .Select(r =>
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<JsonElement>(r.PayloadJson);
                    return new
                    {
                        id = r.Id,
                        year = payload.TryGetProperty("year", out var y) ? y.GetInt32() : 0,
                        month = payload.TryGetProperty("month", out var m) ? m.GetInt32() : 0,
                        status = r.Status,
                        sectionId = payload.TryGetProperty("sectionId", out var sid) && sid.ValueKind != JsonValueKind.Null ? (Guid?)sid.GetGuid() : null,
                        hangarId = payload.TryGetProperty("hangarId", out var hid) && hid.ValueKind != JsonValueKind.Null ? (Guid?)hid.GetGuid() : null,
                        teamLeaderId = payload.TryGetProperty("teamLeaderId", out var tlid) && tlid.ValueKind != JsonValueKind.Null ? (Guid?)tlid.GetGuid() : null,
                        entries = payload.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array
                            ? entries.EnumerateArray().Select(e => new
                            {
                                employeeId = e.TryGetProperty("employeeId", out var eid) ? eid.GetString() : "",
                                employeeName = e.TryGetProperty("employeeName", out var ename) ? ename.GetString() : "",
                                date = e.TryGetProperty("date", out var date) ? date.GetString() : "",
                                shiftType = e.TryGetProperty("shiftType", out var shift) ? shift.GetString() : "Day",
                                isAceActivity = e.TryGetProperty("isAceActivity", out var ace) ? ace.GetBoolean() : false
                            }).ToArray()
                            : Array.Empty<object>()
                    };
                }
                catch
                {
                    return null;
                }
            })
            .Where(x => x is not null)
            .ToList();

        Console.WriteLine($"[ScheduleController] GetAllMonthlySchedules: found {schedules.Count} schedules for userId={userId}, role={currentUser.Role}");

        return Ok(new { schedules });
    }

    // ==================== MONTHLY SCHEDULE DETAIL ENDPOINT ====================

    [HttpGet("monthly-schedules/{id}")]
    public async Task<IActionResult> GetMonthlyScheduleDetail(Guid id, CancellationToken cancellationToken = default)
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
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "schedule" && x.Resource == "monthly", cancellationToken);
        
        if (record is null) return NotFound();

        // Parse payload for RBAC check
        JsonElement? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
        }
        catch
        {
            return BadRequest("Failed to parse schedule data");
        }

        Guid? recordSectionId = null;
        Guid? recordHangarId = null;
        Guid? recordShopId = null;
        Guid? recordTeamLeaderId = null;

        if (payload.HasValue)
        {
            if (payload.Value.TryGetProperty("sectionId", out var sid) && sid.ValueKind != JsonValueKind.Null)
                recordSectionId = sid.GetGuid();
            if (payload.Value.TryGetProperty("hangarId", out var hid) && hid.ValueKind != JsonValueKind.Null)
                recordHangarId = hid.GetGuid();
            if (payload.Value.TryGetProperty("shopId", out var shid) && shid.ValueKind != JsonValueKind.Null)
                recordShopId = shid.GetGuid();
            if (payload.Value.TryGetProperty("teamLeaderId", out var tlid) && tlid.ValueKind != JsonValueKind.Null)
                recordTeamLeaderId = tlid.GetGuid();
        }

        // Apply RBAC based on role
        bool hasAccess = currentUser.Role switch
        {
            UserRole.Employee => recordTeamLeaderId == userId || 
                                (currentUser.ReportsToUserId.HasValue && recordTeamLeaderId == currentUser.ReportsToUserId.Value) ||
                                (currentUser.HangarId.HasValue && recordHangarId == currentUser.HangarId) ||
                                (currentUser.ShopId.HasValue && recordShopId == currentUser.ShopId),
            UserRole.TeamLeader => recordTeamLeaderId == userId ||
                                (currentUser.HangarId.HasValue && recordHangarId == currentUser.HangarId) ||
                                (currentUser.ShopId.HasValue && recordShopId == currentUser.ShopId),
            UserRole.Manager => currentUser.SectionId.HasValue && recordSectionId == currentUser.SectionId,
            UserRole.Director or UserRole.SystemAdmin => true,
            _ => false
        };

        if (!hasAccess)
            return Forbid("You do not have access to this schedule");

        try
        {
            var year = payload.HasValue && payload.Value.TryGetProperty("year", out var y) ? y.GetInt32() : 0;
            var month = payload.HasValue && payload.Value.TryGetProperty("month", out var m) ? m.GetInt32() : 0;
            var status = record.Status;
            
            var entries = new List<object>();
            if (payload.HasValue && payload.Value.TryGetProperty("entries", out var entriesProp) && entriesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in entriesProp.EnumerateArray())
                {
                    entries.Add(new
                    {
                        date = entry.TryGetProperty("date", out var d) ? d.GetString() : "",
                        employeeId = entry.TryGetProperty("employeeId", out var eid) ? eid.GetString() : "",
                        employeeName = entry.TryGetProperty("employeeName", out var ename) ? ename.GetString() : "",
                        shift = entry.TryGetProperty("shiftType", out var shift) ? shift.GetString() : "Day",
                        taskType = entry.TryGetProperty("isAceActivity", out var ace) && ace.GetBoolean() ? "ACE" : "Regular"
                    });
                }
            }

            return Ok(new
            {
                id = record.Id,
                createdAt = record.CreatedAt,
                updatedAt = record.UpdatedAt,
                year,
                month,
                section = recordSectionId?.ToString() ?? "",
                hangar = recordHangarId?.ToString() ?? "",
                status,
                createdBy = new
                {
                    id = record.CreatedBy?.ToString() ?? "",
                    name = "" // Would need to fetch from Users table
                },
                entries,
                notes = ""
            });
        }
        catch
        {
            return BadRequest("Failed to parse schedule data");
        }
    }

    // ==================== MONTHLY SCHEDULE PUBLISH ENDPOINT ====================

    [HttpPost("monthly-schedules/{id}/publish")]
    public async Task<IActionResult> PublishMonthlySchedule(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var currentUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        
        if (currentUser is null) return Unauthorized();
        
        if (currentUser.Role != UserRole.TeamLeader)
            return Forbid("Only Team Leaders can publish monthly schedules");

        var record = await dbContext.OperationalRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.Module == "schedule" && x.Resource == "monthly", cancellationToken);
        
        if (record is null) return NotFound();

        // Verify the team leader created this schedule
        if (record.CreatedBy != userId)
            return Forbid("You can only publish schedules you created");

        record.Status = "Published";
        record.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        
        return Ok(new { success = true, message = "Monthly schedule published successfully" });
    }

    // ==================== MANPOWER SUMMARY ENDPOINT ====================

    [HttpGet("monthly-schedules/manpower-summary")]
    public async Task<IActionResult> GetManpowerSummary(
        [FromQuery] Guid? sectionId, 
        [FromQuery] Guid? hangarId, 
        [FromQuery] Guid? shopId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        CancellationToken cancellationToken = default)
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
            .Where(x => x.Module == "schedule" && x.Resource == "monthly");

        // Apply RBAC filters (same as GetMonthlySchedule)
        if (currentUser.Role == UserRole.Employee)
        {
            if (currentUser.ReportsToUserId.HasValue)
            {
                query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == currentUser.ReportsToUserId.Value);
            }
            else
            {
                var teamLeaderIds = await dbContext.Users
                    .AsNoTracking()
                    .Where(x => x.Role == UserRole.TeamLeader &&
                                ((currentUser.HangarId.HasValue && x.HangarId == currentUser.HangarId) ||
                                 (currentUser.ShopId.HasValue && x.ShopId == currentUser.ShopId)))
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (teamLeaderIds.Count > 0)
                {
                    query = query.Where(x => x.CreatedBy.HasValue && teamLeaderIds.Contains(x.CreatedBy.Value));
                }
                else
                {
                    query = query.Where(x => false);
                }
            }
        }
        else if (currentUser.Role == UserRole.TeamLeader)
        {
            var employeeIdsInScope = await dbContext.Users
                .AsNoTracking()
                .Where(x => x.ReportsToUserId == userId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (employeeIdsInScope.Any())
            {
                // Filter schedules where entries contain employees reporting to this TL
                // Note: PayloadJson filtering must be done in-memory since EF Core can't translate JSON queries
                var filteredRecords = await query.ToListAsync(cancellationToken);
                filteredRecords = filteredRecords.Where(x => x.PayloadJson != null && 
                    employeeIdsInScope.Any(eid => x.PayloadJson.Contains($"\"employeeId\":\"{eid}\""))).ToList();
                
                var recordCount = filteredRecords.Count;
                Console.WriteLine($"[ScheduleController] Query found {recordCount} OperationalRecords for module=schedule, resource=weekly");
                
                // Manpower summary doesn't use pagination, return all filtered records
                var schedules = filteredRecords
                    .Where(r => r.PayloadJson != null)
                    .Select(r =>
                    {
                        try
                        {
                            var payload = JsonSerializer.Deserialize<JsonElement>(r.PayloadJson);
                            return new
                            {
                                id = r.Id,
                                week = payload.TryGetProperty("week", out var w) ? w.GetInt32() : 0,
                                year = payload.TryGetProperty("year", out var y) ? y.GetInt32() : 0,
                                status = r.Status,
                                createdAt = r.CreatedAt,
                                updatedAt = r.UpdatedAt,
                                createdBy = r.CreatedBy,
                                payload = r.PayloadJson
                            };
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(x => x != null)
                    .ToList();

                return Ok(schedules);
            }
            else
            {
                if (currentUser.HangarId.HasValue)
                {
                    var hangarTeamLeaderIds = dbContext.Users
                        .Where(x => x.HangarId == currentUser.HangarId && x.Role == UserRole.TeamLeader)
                        .Select(x => x.Id)
                        .ToList();
                    query = query.Where(x => x.CreatedBy.HasValue && hangarTeamLeaderIds.Contains(x.CreatedBy.Value));
                }
                else if (currentUser.ShopId.HasValue)
                {
                    var shopTeamLeaderIds = dbContext.Users
                        .Where(x => x.ShopId == currentUser.ShopId && x.Role == UserRole.TeamLeader)
                        .Select(x => x.Id)
                        .ToList();
                    query = query.Where(x => x.CreatedBy.HasValue && shopTeamLeaderIds.Contains(x.CreatedBy.Value));
                }
                else
                {
                    query = query.Where(x => x.CreatedBy.HasValue && x.CreatedBy.Value == userId);
                }
            }
        }
        else if (currentUser.Role == UserRole.Manager)
        {
            if (currentUser.SectionId.HasValue)
            {
                var sectionUserIds = dbContext.Users
                    .Where(x => x.SectionId == currentUser.SectionId)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && sectionUserIds.Contains(x.CreatedBy.Value));
                
                if (hangarId.HasValue)
                {
                    var hangarUserIds = dbContext.Users
                        .Where(x => x.HangarId == hangarId.Value && x.SectionId == currentUser.SectionId)
                        .Select(x => x.Id)
                        .ToList();
                    query = query.Where(x => x.CreatedBy.HasValue && hangarUserIds.Contains(x.CreatedBy.Value));
                }
            }
        }
        else if (currentUser.Role == UserRole.Director || currentUser.Role == UserRole.SystemAdmin)
        {
            if (sectionId.HasValue)
            {
                var sectionUserIds = dbContext.Users
                    .Where(x => x.SectionId == sectionId.Value)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && sectionUserIds.Contains(x.CreatedBy.Value));
            }
            if (hangarId.HasValue)
            {
                var hangarUserIds = dbContext.Users
                    .Where(x => x.HangarId == hangarId.Value)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && hangarUserIds.Contains(x.CreatedBy.Value));
            }
            if (shopId.HasValue)
            {
                var shopUserIds = dbContext.Users
                    .Where(x => x.ShopId == shopId.Value)
                    .Select(x => x.Id)
                    .ToList();
                query = query.Where(x => x.CreatedBy.HasValue && shopUserIds.Contains(x.CreatedBy.Value));
            }
        }

        var records = await query.OrderByDescending(x => x.UpdatedAt).ToListAsync(cancellationToken);

        var manpowerByDate = new Dictionary<string, object>();

        foreach (var record in records)
        {
            if (record.PayloadJson == null) continue;

            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
                if (!payload.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var entry in entries.EnumerateArray())
                {
                    var date = entry.TryGetProperty("date", out var d) ? d.GetString() : "";
                    var shiftType = entry.TryGetProperty("shiftType", out var s) ? s.GetString() : "Day";

                    if (string.IsNullOrEmpty(date)) continue;

                    // Apply date range filter if provided
                    if (!string.IsNullOrEmpty(startDate) && string.Compare(date, startDate) < 0) continue;
                    if (!string.IsNullOrEmpty(endDate) && string.Compare(date, endDate) > 0) continue;

                    if (!manpowerByDate.ContainsKey(date))
                    {
                        manpowerByDate[date] = new
                        {
                            date,
                            hangarManpower = 0,
                            sectionManpower = 0,
                            shiftCounts = new Dictionary<string, int>
                            {
                                ["Day"] = 0, ["Night"] = 0, ["Evening"] = 0,
                                ["LongNight"] = 0, ["DayOff"] = 0, ["Vacation"] = 0
                            }
                        };
                    }

                    var summary = (dynamic)manpowerByDate[date];
                    
                    if (summary.shiftCounts.ContainsKey(shiftType))
                        summary.shiftCounts[shiftType]++;

                    if (shiftType != "DayOff" && shiftType != "Vacation")
                    {
                        summary.hangarManpower++;
                        summary.sectionManpower++;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return Ok(manpowerByDate.Values.OrderBy(x => ((dynamic)x).date).ToList());
    }

    // ==================== EMPLOYEE AVAILABILITY ENDPOINT ====================

    [HttpGet("dailyassignment/available-employees")]
    public async Task<IActionResult> GetAvailableEmployees(
        [FromQuery] string date,
        [FromQuery] Guid? hangarId,
        [FromQuery] Guid? shopId,
        CancellationToken cancellationToken = default)
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

        // Only Team Leaders can query available employees
        if (currentUser.Role != UserRole.TeamLeader)
            return Forbid("Only Team Leaders can query available employees");

        // Use Team Leader's hangar/shop if not provided
        var targetHangarId = hangarId ?? currentUser.HangarId;
        var targetShopId = shopId ?? currentUser.ShopId;

        if (!targetHangarId.HasValue && !targetShopId.HasValue)
            return BadRequest("Hangar or Shop must be specified");

        // Get employees in the same hangar/shop
        var employees = await dbContext.Users
            .AsNoTracking()
            .Where(x => (targetHangarId.HasValue && x.HangarId == targetHangarId.Value) ||
                        (targetShopId.HasValue && x.ShopId == targetShopId.Value))
            .ToListAsync(cancellationToken);

        // Get monthly schedules for the date to check availability
        var schedules = await dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "schedule" && x.Resource == "monthly" && x.PayloadJson != null)
            .ToListAsync(cancellationToken);

        var availabilityMap = new Dictionary<Guid, (string shiftType, bool isAvailable)>();

        foreach (var schedule in schedules)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(schedule.PayloadJson);
                if (!payload.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var entry in entries.EnumerateArray())
                {
                    var entryDate = entry.TryGetProperty("date", out var d) ? d.GetString() : "";
                    var employeeId = entry.TryGetProperty("employeeId", out var eid) && eid.ValueKind != JsonValueKind.Null ? (Guid?)eid.GetGuid() : null;
                    var shiftType = entry.TryGetProperty("shiftType", out var s) ? s.GetString() : "Day";

                    if (entryDate == date && employeeId.HasValue)
                    {
                        var isAvailable = shiftType != "DayOff" && shiftType != "Vacation";
                        availabilityMap[employeeId.Value] = (shiftType ?? "Unknown", isAvailable);
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        var result = employees.Select(e => new
        {
            id = e.Id,
            name = e.FullName,
            position = e.Role.ToString(),
            hangarId = e.HangarId,
            shopId = e.ShopId,
            reportsToUserId = e.ReportsToUserId,
            availability = availabilityMap.TryGetValue(e.Id, out var avail)
                ? new { shiftType = avail.shiftType, isAvailable = avail.isAvailable }
                : new { shiftType = "", isAvailable = true } // Default to available if no schedule found
        }).ToList();

        return Ok(new { employees = result });
    }

    // ==================== WORKLOAD ANALYTICS ENDPOINT ====================

    [HttpGet("dailyassignment/analytics/workload")]
    public async Task<IActionResult> GetWorkloadAnalytics(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] Guid? sectionId,
        [FromQuery] Guid? hangarId,
        [FromQuery] Guid? shopId,
        CancellationToken cancellationToken = default)
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

        // Managers and Directors can view analytics
        if (currentUser.Role != UserRole.Manager && currentUser.Role != UserRole.Director && currentUser.Role != UserRole.SystemAdmin)
            return Forbid("Only Managers and Directors can view workload analytics");

        var query = dbContext.OperationalRecords
            .AsNoTracking()
            .Where(x => x.Module == "dailyassignment" && x.Resource == "assignment");

        // Apply RBAC filters
        if (currentUser.Role == UserRole.Manager)
        {
            if (currentUser.SectionId.HasValue)
            {
                var sectionIdStr = currentUser.SectionId.Value.ToString();
                query = query.Where(x => x.PayloadJson != null && 
                    x.PayloadJson.Contains($"\"sectionId\":\"{sectionIdStr}\""));
            }
        }

        // Apply date filters
        if (!string.IsNullOrEmpty(startDate))
        {
            query = query.Where(x => x.PayloadJson != null &&
                x.PayloadJson.Contains($"\"date\":\"{startDate}\""));
        }
        if (!string.IsNullOrEmpty(endDate))
        {
            // For simplicity, this is a basic date filter
            // In production, use proper date range comparison
        }
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

        var records = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

        var assignmentsByDate = new Dictionary<string, int>();
        var assignmentsByStatus = new Dictionary<string, int>();
        var assignmentsByHangar = new Dictionary<string, int>();
        var assignmentsByShop = new Dictionary<string, int>();
        var totalAssignedManpower = 0;
        var totalExpectedManpower = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
                var date = payload.TryGetProperty("date", out var d) ? d.GetString() : "";
                var status = record.Status;
                var expectedManpower = payload.TryGetProperty("expectedManpower", out var em) ? em.GetInt32() : 0;
                var recordHangarId = payload.TryGetProperty("hangarId", out var hid) && hid.ValueKind != JsonValueKind.Null ? hid.GetGuid().ToString() : null;
                var recordShopId = payload.TryGetProperty("shopId", out var shid) && shid.ValueKind != JsonValueKind.Null ? shid.GetGuid().ToString() : null;

                // Count by date
                if (!string.IsNullOrEmpty(date))
                {
                    if (!assignmentsByDate.ContainsKey(date))
                        assignmentsByDate[date] = 0;
                    assignmentsByDate[date]++;
                }

                // Count by status
                if (!string.IsNullOrEmpty(status))
                {
                    if (!assignmentsByStatus.ContainsKey(status))
                        assignmentsByStatus[status] = 0;
                    assignmentsByStatus[status]++;
                }

                // Count by hangar
                if (recordHangarId != null)
                {
                    if (!assignmentsByHangar.ContainsKey(recordHangarId))
                        assignmentsByHangar[recordHangarId] = 0;
                    assignmentsByHangar[recordHangarId]++;
                }

                // Count by shop
                if (recordShopId != null)
                {
                    if (!assignmentsByShop.ContainsKey(recordShopId))
                        assignmentsByShop[recordShopId] = 0;
                    assignmentsByShop[recordShopId]++;
                }

                // Manpower calculations
                if (payload.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
                {
                    var assignedCount = details.GetArrayLength();
                    totalAssignedManpower += assignedCount;
                }
                totalExpectedManpower += expectedManpower;
            }
            catch
            {
                continue;
            }
        }

        // Calculate utilization rate
        double utilizationRate = 0.0;
        if (totalExpectedManpower > 0)
        {
            utilizationRate = (double)totalAssignedManpower / totalExpectedManpower * 100;
        }

        var analytics = new
        {
            totalAssignments = records.Count,
            assignmentsByDate,
            assignmentsByStatus,
            assignmentsByHangar,
            assignmentsByShop,
            totalAssignedManpower,
            totalExpectedManpower,
            utilizationRate
        };

        return Ok(analytics);
    }
}
