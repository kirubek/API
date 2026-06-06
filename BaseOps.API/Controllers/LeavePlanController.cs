using BaseOps.Application.DTOs;
using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Infrastructure.Data;
using BaseOps.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LeavePlanController(
    BaseOpsDbContext dbContext,
    ICompletenessValidator completenessValidator,
    IAllocationEngine allocationEngine,
    IAuditService auditService,
    IManpowerConstraintService manpowerConstraintService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
    private string CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedAccessException();

    [HttpPost("plan/generate")]
    [Authorize(Policy = "AnnualLeaveGenerate")]
    public async Task<IActionResult> GeneratePlan([FromBody] GeneratePlanDto request, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"[DEBUG] GeneratePlan called with: Level={request.Level}, SectionId={request.SectionId}, HangarId={request.HangarId}, ShopId={request.ShopId}, Year={request.Year}");

        // Validate completeness first
        var statusRequest = new AnnualLeaveStatusRequestDto
        {
            Level = request.Level,
            SectionId = request.SectionId ?? Guid.Empty,
            HangarId = request.HangarId,
            ShopId = request.ShopId,
            TeamLeaderId = request.TeamLeaderId,
            Year = request.Year
        };

        var completenessStatus = await completenessValidator.ValidateCompletenessAsync(statusRequest, cancellationToken);
        System.Console.WriteLine($"[DEBUG] Completeness status: CanGeneratePlan={completenessStatus.CanGeneratePlan}, TotalRequired={completenessStatus.TotalRequired}, TotalSubmitted={completenessStatus.TotalSubmitted}");
        
        if (!completenessStatus.CanGeneratePlan)
        {
            return BadRequest(completenessStatus);
        }

        // Get leave requests for the scope first to identify their IDs
        var requestsQuery = dbContext.AnnualLeaveRequests
            .AsNoTracking()
            .Where(r => r.Year == request.Year && r.Status == AnnualLeaveRequestStatus.Submitted);

        // Filter by scope based on level
        if (request.Level == AnnualLeavePlanLevel.TeamLeader)
        {
            if (request.HangarId.HasValue)
            {
                requestsQuery = requestsQuery.Where(r => r.HangarId == request.HangarId.Value);
            }
            if (request.ShopId.HasValue)
            {
                requestsQuery = requestsQuery.Where(r => r.ShopId == request.ShopId.Value);
            }
        }
        else if (request.Level == AnnualLeavePlanLevel.Manager)
        {
            requestsQuery = requestsQuery.Where(r => r.SectionId == request.SectionId);
        }
        else if (request.Level == AnnualLeavePlanLevel.Director)
        {
            requestsQuery = requestsQuery.Where(r => r.RoleAtSubmission == RoleAtSubmission.Manager);
        }

        var requestIds = (await requestsQuery.Select(r => r.Id).ToListAsync(cancellationToken)).ToList();

        // Delete all plan entries for these requests to avoid duplicate key violations
        if (requestIds.Any())
        {
            var existingEntries = dbContext.AnnualLeavePlanEntries
                .Where(e => requestIds.Contains(e.AnnualLeaveRequestId))
                .ToList();
            
            if (existingEntries.Any())
            {
                System.Console.WriteLine($"[DEBUG] Deleting {existingEntries.Count} existing plan entries");
                dbContext.AnnualLeavePlanEntries.RemoveRange(existingEntries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // Delete any existing draft plans for the same scope/year
        var existingDraftPlans = dbContext.AnnualLeavePlans
            .Where(p => p.Year == request.Year 
                && p.Status == AnnualLeavePlanStatus.Draft
                && p.Level == request.Level
                && (request.SectionId.HasValue ? p.SectionId == request.SectionId.Value : true)
                && (request.HangarId.HasValue ? p.HangarId == request.HangarId.Value : true)
                && (request.ShopId.HasValue ? p.ShopId == request.ShopId.Value : true))
            .ToList();
        
        if (existingDraftPlans.Any())
        {
            System.Console.WriteLine($"[DEBUG] Deleting {existingDraftPlans.Count} existing draft plans");
            dbContext.AnnualLeavePlans.RemoveRange(existingDraftPlans);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Get leave requests for the scope
        var requests = dbContext.AnnualLeaveRequests
            .Include(r => r.LeaveChoices)
            .Include(r => r.User)
            .AsNoTracking()
            .Where(r => r.Year == request.Year && r.Status == AnnualLeaveRequestStatus.Submitted);

        // Filter by scope based on level
        if (request.Level == AnnualLeavePlanLevel.TeamLeader)
        {
            if (request.HangarId.HasValue)
            {
                requests = requests.Where(r => r.HangarId == request.HangarId.Value);
            }
            if (request.ShopId.HasValue)
            {
                requests = requests.Where(r => r.ShopId == request.ShopId.Value);
            }
        }
        else if (request.Level == AnnualLeavePlanLevel.Manager)
        {
            // Managers should only see Team Leader requests, not Employee requests
            requests = requests.Where(r => r.SectionId == request.SectionId && r.RoleAtSubmission == RoleAtSubmission.TeamLeader);
        }
        else if (request.Level == AnnualLeavePlanLevel.Director)
        {
            requests = requests.Where(r => r.RoleAtSubmission == RoleAtSubmission.Manager);
        }

        var requestsList = await requests.ToListAsync(cancellationToken);

        // Generate plan entries using allocation engine
        var planEntries = await allocationEngine.GeneratePlanEntriesAsync(requestsList, request, cancellationToken);

        // For TeamLeader level, always get SectionId from the team leader's user record
        // to avoid foreign key constraint violations with invalid SectionId values
        Guid sectionId = request.SectionId ?? Guid.Empty;
        if (request.Level == AnnualLeavePlanLevel.TeamLeader && request.TeamLeaderId.HasValue)
        {
            var teamLeader = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == request.TeamLeaderId.Value)
                .Select(u => u.SectionId)
                .FirstOrDefaultAsync(cancellationToken);
            if (teamLeader.HasValue && teamLeader.Value != Guid.Empty)
            {
                sectionId = teamLeader.Value;
                Console.WriteLine($"[DEBUG] Using SectionId from team leader: {sectionId}");
            }
            else
            {
                Console.WriteLine($"[DEBUG] Team leader has no valid SectionId, cannot generate plan");
                return BadRequest(new { message = "Team leader must be assigned to a valid section to generate a leave plan." });
            }
        }

        // Calculate total employees for the scope (excluding team leader for TeamLeader level plans)
        var totalEmployees = await GetTotalEmployeesForPlanAsync(new AnnualLeavePlan
        {
            Level = request.Level,
            SectionId = sectionId,
            HangarId = request.HangarId,
            ShopId = request.ShopId,
            TeamLeaderId = request.TeamLeaderId
        }, cancellationToken);

        // Create the plan
        var plan = new AnnualLeavePlan
        {
            Level = request.Level,
            SectionId = sectionId,
            HangarId = request.HangarId,
            ShopId = request.ShopId,
            TeamLeaderId = request.TeamLeaderId,
            Year = request.Year,
            Status = AnnualLeavePlanStatus.Draft,
            CreatedBy = CurrentUserId,
            TotalEmployees = totalEmployees,
            TotalOnLeave = planEntries.Count(e => e.ApprovedStartDate > DateTimeOffset.MinValue),
            TotalAvailable = totalEmployees - planEntries.Count(e => e.ApprovedStartDate > DateTimeOffset.MinValue),
            GenerationNotes = completenessStatus.Message
        };

        // Link entries to plan
        foreach (var entry in planEntries)
        {
            entry.AnnualLeavePlanId = plan.Id;
            entry.CreatedBy = CurrentUserId;
        }

        plan.Entries = planEntries;

        // Save to database
        dbContext.AnnualLeavePlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Reload the plan with User navigation properties for proper DTO mapping
        var savedPlan = await dbContext.AnnualLeavePlans
            .Include(p => p.Entries)
            .ThenInclude(e => e.User)
            .Include(p => p.Section)
            .Include(p => p.Hangar)
            .Include(p => p.Shop)
            .Include(p => p.TeamLeader)
            .FirstOrDefaultAsync(p => p.Id == plan.Id, cancellationToken);

        // Audit logging
        await auditService.WriteAsync(
            CurrentUserId,
            "AnnualLeavePlanGenerated",
            "AnnualLeavePlan",
            plan.Id.ToString(),
            null,
            new { Level = plan.Level, Year = plan.Year, TotalEmployees = plan.TotalEmployees, TotalOnLeave = plan.TotalOnLeave },
            false,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.TraceIdentifier,
            cancellationToken);

        var dto = MapToDto(savedPlan!);
        return Ok(dto);
    }

    [HttpGet("plan/{planId}")]
    [Authorize(Policy = "AnnualLeaveViewTeam")]
    public async Task<IActionResult> GetPlan(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.AnnualLeavePlans
            .Include(p => p.Entries)
            .ThenInclude(e => e.User)
            .Include(p => p.Section)
            .Include(p => p.Hangar)
            .Include(p => p.Shop)
            .Include(p => p.TeamLeader)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

        if (plan == null)
        {
            return NotFound();
        }

        // Authorization check
        if (!CanAccessPlan(plan))
        {
            return Forbid();
        }

        var dto = MapToDto(plan);
        return Ok(dto);
    }

    [HttpGet("my-plan")]
    [Authorize(Policy = "AnnualLeaveViewOwn")]
    public async Task<IActionResult> GetMyApprovedPlan(CancellationToken cancellationToken = default)
    {
        var currentYear = DateTime.UtcNow.Year;
        
        // Find finalized team leader plans for the user's hangar/shop
        var currentUser = await dbContext.Users.FindAsync([CurrentUserId], cancellationToken);
        if (currentUser == null)
        {
            return NotFound("User not found");
        }

        var finalizedPlans = await dbContext.AnnualLeavePlans
            .Include(p => p.Entries)
            .ThenInclude(e => e.User)
            .Include(p => p.Hangar)
            .Include(p => p.Shop)
            .Where(p => p.Year == currentYear 
                && p.Level == AnnualLeavePlanLevel.TeamLeader
                && p.Status == AnnualLeavePlanStatus.Finalized
                && p.HangarId == currentUser.HangarId
                && p.ShopId == currentUser.ShopId)
            .ToListAsync(cancellationToken);

        // Find the user's approved entry from any of these plans
        var approvedEntry = finalizedPlans
            .SelectMany(p => p.Entries)
            .FirstOrDefault(e => e.UserId == CurrentUserId);

        if (approvedEntry == null)
        {
            return NotFound("No approved leave plan found for you");
        }

        var dto = new
        {
            approvedEntry.Id,
            approvedEntry.UserId,
            approvedEntry.User?.FullName,
            approvedEntry.User?.EmployeeId,
            approvedEntry.ApprovedStartDate,
            approvedEntry.ApprovedEndDate,
            approvedEntry.SourceChoice,
            approvedEntry.PriorityScore,
            approvedEntry.IsManuallyAdjusted,
            approvedEntry.SplitIndex
        };

        return Ok(dto);
    }

    [HttpGet("plan")]
    [Authorize(Policy = "AnnualLeaveViewTeam")]
    public async Task<IActionResult> ListPlans([FromQuery] AnnualLeavePlanLevel? level, [FromQuery] Guid? sectionId, [FromQuery] Guid? hangarId, [FromQuery] Guid? shopId, [FromQuery] Guid? teamLeaderId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AnnualLeavePlans
            .Include(p => p.Entries)
            .ThenInclude(e => e.User)
            .Include(p => p.Section)
            .Include(p => p.Hangar)
            .Include(p => p.Shop)
            .Include(p => p.TeamLeader)
            .AsNoTracking();

        // Authorization filtering
        if (CurrentUserRole == "TeamLeader")
        {
            var currentUser = await dbContext.Users.FindAsync([CurrentUserId], cancellationToken);
            if (currentUser == null) return Unauthorized();
            
            // If teamLeaderId is provided, filter by that (for viewing specific team leader's plans)
            if (teamLeaderId.HasValue)
            {
                // Only allow viewing own plans or plans where they are the team leader
                if (teamLeaderId.Value != CurrentUserId)
                {
                    return Forbid();
                }
                query = query.Where(p => p.TeamLeaderId == teamLeaderId.Value);
            }
            else
            {
                // Match by hangarId, and shopId if both user and plan have one
                if (currentUser.ShopId.HasValue)
                {
                    query = query.Where(p => p.HangarId == currentUser.HangarId && p.ShopId == currentUser.ShopId);
                }
                else
                {
                    query = query.Where(p => p.HangarId == currentUser.HangarId && p.ShopId == null);
                }
            }
        }
        else if (CurrentUserRole == "Manager")
        {
            var currentUser = await dbContext.Users.FindAsync([CurrentUserId], cancellationToken);
            if (currentUser == null) return Unauthorized();
            query = query.Where(p => p.SectionId == currentUser.SectionId);
        }
        // Directors can view all

        // Apply filters
        if (level.HasValue)
        {
            query = query.Where(p => p.Level == level.Value);
        }
        if (sectionId.HasValue)
        {
            query = query.Where(p => p.SectionId == sectionId.Value);
        }
        if (hangarId.HasValue)
        {
            query = query.Where(p => p.HangarId == hangarId.Value);
        }

        var plans = await query.ToListAsync(cancellationToken);
        var dtos = plans.Select(MapToDto).ToList();

        return Ok(dtos);
    }

    [HttpPut("plan/{planId}/adjust")]
    [Authorize(Policy = "AnnualLeaveAdjust")]
    public async Task<IActionResult> AdjustPlan(Guid planId, [FromBody] AdjustPlanEntryDto[] adjustments, CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.AnnualLeavePlans
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

        if (plan == null)
        {
            return NotFound();
        }

        // Authorization check
        if (!CanModifyPlan(plan))
        {
            return Forbid();
        }

        // Check if plan is finalized
        if (plan.Status == AnnualLeavePlanStatus.Finalized)
        {
            return BadRequest("Cannot adjust a finalized plan");
        }

        // Get current manpower constraints for validation
        var constraints = await manpowerConstraintService.GetConstraintsAsync(
            plan.SectionId,
            plan.HangarId,
            plan.ShopId,
            plan.Year,
            cancellationToken);

        // Validate each adjustment against constraints
        foreach (var adjustment in adjustments)
        {
            var entry = plan.Entries.FirstOrDefault(e => e.Id == adjustment.EntryId);
            if (entry == null) continue;

            var startDate = adjustment.ApprovedStartDate;
            var endDate = adjustment.ApprovedEndDate;

            // Check each date in the adjustment range
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Count other entries on this date (excluding current entry being adjusted)
                var currentCount = plan.Entries
                    .Where(e => e.Id != entry.Id)
                    .Count(e => {
                        var entryStart = DateOnly.FromDateTime(e.ApprovedStartDate.DateTime);
                        var entryEnd = DateOnly.FromDateTime(e.ApprovedEndDate.DateTime);
                        return date >= entryStart && date <= entryEnd;
                    });

                // Check against max leave count
                var maxAllowed = constraints.MaxLeaveCount ?? 
                    (int)Math.Ceiling(await GetTotalEmployeesForPlanAsync(plan, cancellationToken) * constraints.MaxLeavePercentage);
                
                if (currentCount + 1 > maxAllowed)
                {
                    return BadRequest($"Adjustment would exceed maximum leave count of {maxAllowed} on {date:yyyy-MM-dd}");
                }

                // Check against minimum coverage
                var minRequired = constraints.MinCoverageCount ??
                    (int)Math.Ceiling(await GetTotalEmployeesForPlanAsync(plan, cancellationToken) * constraints.MinCoveragePercentage);
                
                var totalEmployees = await GetTotalEmployeesForPlanAsync(plan, cancellationToken);
                if (totalEmployees - (currentCount + 1) < minRequired)
                {
                    return BadRequest($"Adjustment would leave below minimum coverage of {minRequired} on {date:yyyy-MM-dd}");
                }
            }
        }

        // Apply adjustments
        foreach (var adjustment in adjustments)
        {
            var entry = plan.Entries.FirstOrDefault(e => e.Id == adjustment.EntryId);
            if (entry != null)
            {
                entry.ApprovedStartDate = adjustment.ApprovedStartDate.ToDateTime(TimeOnly.MinValue);
                entry.ApprovedEndDate = adjustment.ApprovedEndDate.ToDateTime(TimeOnly.MinValue);
                entry.IsManuallyAdjusted = true;
                entry.ManuallyAdjustedAt = DateTimeOffset.UtcNow;
                entry.ManuallyAdjustedByUserId = CurrentUserId;
                entry.AdjustmentReason = adjustment.AdjustmentReason;
                entry.UpdatedBy = CurrentUserId;
                entry.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Audit logging
        await auditService.WriteAsync(
            CurrentUserId,
            "AnnualLeavePlanAdjusted",
            "AnnualLeavePlan",
            planId.ToString(),
            null,
            new { AdjustmentsCount = adjustments.Length },
            false,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            HttpContext.TraceIdentifier,
            cancellationToken);

        var dto = MapToDto(plan);
        return Ok(dto);
    }

    [HttpPost("plan/{planId}/finalize")]
    [Authorize(Policy = "AnnualLeaveFinalize")]
    public async Task<IActionResult> FinalizePlan(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.AnnualLeavePlans
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

        if (plan == null)
        {
            return NotFound();
        }

        // Authorization check
        if (!CanModifyPlan(plan))
        {
            return Forbid();
        }

        // Check if already finalized
        if (plan.Status == AnnualLeavePlanStatus.Finalized)
        {
            return BadRequest("Plan is already finalized");
        }

        // Finalize the plan
        plan.Status = AnnualLeavePlanStatus.Finalized;
        plan.FinalizedAt = DateTimeOffset.UtcNow;
        plan.FinalizedByUserId = CurrentUserId;
        plan.UpdatedBy = CurrentUserId;
        plan.UpdatedAt = DateTimeOffset.UtcNow;

        // Update all associated leave requests to Approved status
        var requestIds = plan.Entries.Select(e => e.AnnualLeaveRequestId).Distinct().ToList();
        var leaveRequests = await dbContext.AnnualLeaveRequests
            .Where(r => requestIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        foreach (var request in leaveRequests)
        {
            request.Status = AnnualLeaveRequestStatus.Approved;
            request.UpdatedAt = DateTimeOffset.UtcNow;
            request.UpdatedBy = CurrentUserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Audit logging
        await auditService.WriteAsync(
            CurrentUserId,
            "AnnualLeavePlanFinalized",
            "AnnualLeavePlan",
            planId.ToString(),
            null,
            new { Year = plan.Year, Level = plan.Level, TotalEmployees = plan.TotalEmployees, TotalOnLeave = plan.TotalOnLeave },
            false,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.TraceIdentifier,
            cancellationToken);

        var dto = MapToDto(plan);
        return Ok(dto);
    }

    private bool CanAccessPlan(AnnualLeavePlan plan)
    {
        return CurrentUserRole switch
        {
            "Director" => true,
            "Manager" => plan.SectionId == GetUserSectionId(),
            "TeamLeader" => plan.HangarId == GetUserHangarId() && plan.ShopId == GetUserShopId(),
            _ => false
        };
    }

    private bool CanModifyPlan(AnnualLeavePlan plan)
    {
        // Only the creator or users with appropriate role can modify
        if (plan.CreatedBy == CurrentUserId) return true;

        return CurrentUserRole switch
        {
            "Director" => true,
            "Manager" => plan.Level == AnnualLeavePlanLevel.TeamLeader && plan.SectionId == GetUserSectionId(),
            "TeamLeader" => plan.Level == AnnualLeavePlanLevel.TeamLeader && plan.HangarId == GetUserHangarId(),
            _ => false
        };
    }

    private async Task<Guid?> GetUserSectionIdAsync(CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync([CurrentUserId], cancellationToken);
        return user?.SectionId;
    }

    private async Task<Guid?> GetUserHangarIdAsync(CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync([CurrentUserId], cancellationToken);
        return user?.HangarId;
    }

    private async Task<Guid?> GetUserShopIdAsync(CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync([CurrentUserId], cancellationToken);
        return user?.ShopId;
    }

    private Guid? GetUserSectionId()
    {
        // Synchronous version for non-async contexts
        var user = dbContext.Users.Find(CurrentUserId);
        return user?.SectionId;
    }

    private Guid? GetUserHangarId()
    {
        // Synchronous version for non-async contexts
        var user = dbContext.Users.Find(CurrentUserId);
        return user?.HangarId;
    }

    private Guid? GetUserShopId()
    {
        // Synchronous version for non-async contexts
        var user = dbContext.Users.Find(CurrentUserId);
        return user?.ShopId;
    }

    private static AnnualLeavePlanDto MapToDto(AnnualLeavePlan plan)
    {
        return new AnnualLeavePlanDto
        {
            Id = plan.Id,
            Level = plan.Level,
            SectionId = plan.SectionId,
            SectionName = plan.Section?.Name ?? string.Empty,
            HangarId = plan.HangarId,
            HangarName = plan.Hangar?.Name,
            ShopId = plan.ShopId,
            ShopName = plan.Shop?.Name,
            TeamLeaderId = plan.TeamLeaderId,
            TeamLeaderName = plan.TeamLeader?.FullName,
            Year = plan.Year,
            Status = plan.Status,
            CreatedAt = plan.CreatedAt,
            FinalizedAt = plan.FinalizedAt,
            Entries = plan.Entries.Select(e => new AnnualLeavePlanEntryDto
            {
                Id = e.Id,
                UserId = e.UserId,
                UserName = e.User?.FullName ?? string.Empty,
                EmployeeId = e.User?.EmployeeId ?? string.Empty,
                ApprovedStartDate = DateOnly.FromDateTime(e.ApprovedStartDate.DateTime),
                ApprovedEndDate = DateOnly.FromDateTime(e.ApprovedEndDate.DateTime),
                SourceChoice = e.SourceChoice,
                PriorityScore = e.PriorityScore,
                IsManuallyAdjusted = e.IsManuallyAdjusted,
                ManuallyAdjustedAt = e.ManuallyAdjustedAt,
                ManuallyAdjustedByUserName = e.ManuallyAdjustedByUser?.FullName,
                AdjustmentReason = e.AdjustmentReason,
                SplitIndex = e.SplitIndex,
                UserRole = e.User?.Role.ToString()
            }).ToList(),
            TotalEmployees = plan.TotalEmployees,
            TotalOnLeave = plan.TotalOnLeave,
            TotalAvailable = plan.TotalAvailable,
            GenerationNotes = plan.GenerationNotes
        };
    }

    private async Task<int> GetTotalEmployeesForPlanAsync(AnnualLeavePlan plan, CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking().Where(u => u.IsActive);

        if (plan.SectionId != Guid.Empty)
        {
            query = query.Where(u => u.SectionId == plan.SectionId);
        }
        if (plan.HangarId.HasValue)
        {
            query = query.Where(u => u.HangarId == plan.HangarId.Value);
        }
        if (plan.ShopId.HasValue)
        {
            query = query.Where(u => u.ShopId == plan.ShopId.Value);
        }

        // For TeamLeader level plans, exclude the team leader from the count
        if (plan.Level == AnnualLeavePlanLevel.TeamLeader && plan.TeamLeaderId.HasValue)
        {
            query = query.Where(u => u.Id != plan.TeamLeaderId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }
}
