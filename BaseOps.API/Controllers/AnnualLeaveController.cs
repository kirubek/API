using BaseOps.Application.DTOs;
using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
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
public class AnnualLeaveController(
    BaseOpsDbContext dbContext,
    ICompletenessValidator completenessValidator,
    IAnalyticsService analyticsService,
    IAuditService auditService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
    private string CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedAccessException();

    [HttpPost]
    [Authorize(Policy = "AnnualLeaveSubmit")]
    public async Task<IActionResult> SubmitRequest([FromBody] SubmitAnnualLeaveDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user
            var user = await dbContext.Users.FindAsync([CurrentUserId], cancellationToken);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Check for duplicate submission for the year
            var currentYear = DateTime.UtcNow.Year;
            var existingRequest = await dbContext.AnnualLeaveRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.UserId == CurrentUserId && r.Year == currentYear, cancellationToken);

            if (existingRequest != null)
            {
                return BadRequest("You have already submitted a leave request for this year");
            }

            // Determine role at submission
            var roleAtSubmission = DetermineRoleAtSubmission(user);

            // Determine submitted to user based on hierarchy
            var submittedToUser = await DetermineSubmittedToUserAsync(user, roleAtSubmission, cancellationToken);
            if (submittedToUser == null)
            {
                return BadRequest("Unable to determine approval chain. Please contact administrator.");
            }

            // Validate choices
            var validationResult = ValidateChoicesWithDetails(dto, roleAtSubmission);
            if (!validationResult.isValid)
            {
                return BadRequest(new { message = validationResult.errorMessage });
            }

            // Create leave request
            var request = new AnnualLeaveRequest
            {
                UserId = CurrentUserId,
                RoleAtSubmission = roleAtSubmission,
                SectionId = user.SectionId ?? Guid.Empty,
                HangarId = user.HangarId,
                ShopId = user.ShopId,
                SubmittedToUserId = submittedToUser.Id,
                LeaveType = dto.LeaveType,
                Year = currentYear,
                Status = AnnualLeaveRequestStatus.Submitted,
                SubmittedAt = DateTimeOffset.UtcNow
            };

            // Create leave choices
            var choices = new List<LeaveChoice>();
        choices.Add(CreateChoice(request, 1, dto.Choice1StartDate, dto.Choice1EndDate));
        choices.Add(CreateChoice(request, 2, dto.Choice2StartDate, dto.Choice2EndDate));
        choices.Add(CreateChoice(request, 3, dto.Choice3StartDate, dto.Choice3EndDate));

        // Add additional choices for split leave
        if (dto.LeaveType == LeaveType.Split)
        {
            if (dto.Choice4StartDate.HasValue && dto.Choice4EndDate.HasValue)
                choices.Add(CreateChoice(request, 4, dto.Choice4StartDate.Value, dto.Choice4EndDate.Value, 1));
            if (dto.Choice5StartDate.HasValue && dto.Choice5EndDate.HasValue)
                choices.Add(CreateChoice(request, 5, dto.Choice5StartDate.Value, dto.Choice5EndDate.Value, 2));
            if (dto.Choice6StartDate.HasValue && dto.Choice6EndDate.HasValue)
                choices.Add(CreateChoice(request, 6, dto.Choice6StartDate.Value, dto.Choice6EndDate.Value, 3));
        }

        request.LeaveChoices = choices;

        dbContext.AnnualLeaveRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Audit logging
        await auditService.WriteAsync(
            CurrentUserId,
            "AnnualLeaveRequestSubmitted",
            "AnnualLeaveRequest",
            request.Id.ToString(),
            null,
            new { UserId = request.UserId, Year = request.Year, LeaveType = request.LeaveType, Status = request.Status },
            false,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.TraceIdentifier,
            cancellationToken);

        var responseDto = MapToDto(request, user, submittedToUser);
        return CreatedAtAction(nameof(GetMyRequest), new { id = request.Id }, responseDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message, details = ex.StackTrace });
        }
    }

    [HttpGet("my")]
    [Authorize(Policy = "AnnualLeaveViewOwn")]
    public async Task<IActionResult> GetMyRequest(CancellationToken cancellationToken = default)
    {
        var currentYear = DateTime.UtcNow.Year;
        var request = await dbContext.AnnualLeaveRequests
            .Include(r => r.LeaveChoices)
            .Include(r => r.User)
            .Include(r => r.SubmittedToUser)
            .FirstOrDefaultAsync(r => r.UserId == CurrentUserId && r.Year == currentYear && r.Status != AnnualLeaveRequestStatus.Draft, cancellationToken);

        if (request == null)
        {
            return Ok((object?)null);
        }

        // Get approved choice from finalized plan if exists
        var approvedChoice = await dbContext.AnnualLeavePlanEntries
            .Include(e => e.AnnualLeavePlan)
            .Where(e => e.AnnualLeaveRequestId == request.Id && e.AnnualLeavePlan.Status == AnnualLeavePlanStatus.Finalized)
            .Select(e => new
            {
                ChoiceNumber = (int)e.SourceChoice,
                StartDate = e.ApprovedStartDate > DateTimeOffset.MinValue ? DateOnly.FromDateTime(e.ApprovedStartDate.DateTime) : (DateOnly?)null,
                EndDate = e.ApprovedEndDate > DateTimeOffset.MinValue ? DateOnly.FromDateTime(e.ApprovedEndDate.DateTime) : (DateOnly?)null
            })
            .FirstOrDefaultAsync(cancellationToken);

        var dto = MapToDto(request, request.User, request.SubmittedToUser, approvedChoice);
        return Ok(dto);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "AnnualLeaveViewTeam")]
    public async Task<IActionResult> GetRequest(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.AnnualLeaveRequests
            .Include(r => r.LeaveChoices)
            .Include(r => r.User)
            .Include(r => r.SubmittedToUser)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request == null)
        {
            return NotFound();
        }

        // Authorization check: users can only view their own requests
        if (request.UserId != CurrentUserId && CurrentUserRole != "Manager" && CurrentUserRole != "Director")
        {
            return Forbid();
        }

        var dto = MapToDto(request, request.User, request.SubmittedToUser);
        return Ok(dto);
    }

    [HttpGet]
    [Authorize(Policy = "AnnualLeaveViewTeam")]
    public async Task<IActionResult> GetRequests([FromQuery] Guid? sectionId, [FromQuery] Guid? hangarId, [FromQuery] Guid? shopId, [FromQuery] string? status, CancellationToken cancellationToken = default)
    {
        var currentYear = DateTime.UtcNow.Year;
        
        var query = dbContext.AnnualLeaveRequests
            .Include(r => r.LeaveChoices)
            .Include(r => r.User)
            .Include(r => r.SubmittedToUser)
            .AsNoTracking();

        // Authorization: Team Leaders can view requests in their hangar/shop
        // Managers can view requests in their section
        // Directors can view only Manager requests
        if (CurrentUserRole == "TeamLeader")
        {
            var currentUser = await dbContext.Users.FindAsync([CurrentUserId], cancellationToken);
            if (currentUser == null) return Unauthorized();
            // Use AND logic for proper hangar/shop isolation
            if (currentUser.HangarId.HasValue && currentUser.ShopId.HasValue)
            {
                query = query.Where(r => r.HangarId == currentUser.HangarId && r.ShopId == currentUser.ShopId);
            }
            else if (currentUser.HangarId.HasValue)
            {
                query = query.Where(r => r.HangarId == currentUser.HangarId && r.ShopId == null);
            }
            else if (currentUser.ShopId.HasValue)
            {
                query = query.Where(r => r.ShopId == currentUser.ShopId && r.HangarId == null);
            }
            // Exclude the team leader's own request from the list
            query = query.Where(r => r.UserId != CurrentUserId);
        }
        else if (CurrentUserRole == "Manager")
        {
            var currentUser = await dbContext.Users.FindAsync([CurrentUserId], cancellationToken);
            if (currentUser == null) return Unauthorized();
            // Managers should only see Team Leader requests, not Employee requests
            query = query.Where(r => r.SectionId == currentUser.SectionId && r.RoleAtSubmission == RoleAtSubmission.TeamLeader);
            
            // Apply optional filters for hangar and shop
            if (hangarId.HasValue)
            {
                query = query.Where(r => r.HangarId == hangarId.Value);
            }
            if (shopId.HasValue)
            {
                query = query.Where(r => r.ShopId == shopId.Value);
            }
        }
        else if (CurrentUserRole == "Director")
        {
            // Directors should only see Manager requests, not Team Leader or Employee requests
            query = query.Where(r => r.RoleAtSubmission == RoleAtSubmission.Manager);
            Console.WriteLine($"[DEBUG] Director filtering for Manager requests only. Total before filter: {await query.CountAsync(cancellationToken)}");
        }
        else
        {
            // Employees should see nothing in this endpoint
            return Ok(new List<AnnualLeaveRequestDto>());
        }

        // Apply filters
        if (sectionId.HasValue)
        {
            query = query.Where(r => r.SectionId == sectionId.Value);
        }
        if (hangarId.HasValue)
        {
            query = query.Where(r => r.HangarId == hangarId.Value);
        }
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AnnualLeaveRequestStatus>(status, out var statusEnum))
        {
            query = query.Where(r => r.Status == statusEnum);
        }

        var requests = await query.ToListAsync(cancellationToken);
        
        // Get approved choice information from finalized plans
        var requestIds = requests.Select(r => r.Id).ToList();
        var planEntries = await dbContext.AnnualLeavePlanEntries
            .Include(e => e.AnnualLeavePlan)
            .Where(e => requestIds.Contains(e.AnnualLeaveRequestId) && e.AnnualLeavePlan.Status == AnnualLeavePlanStatus.Finalized)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        var approvedChoicesMap = planEntries.ToDictionary(e => e.AnnualLeaveRequestId, e => new {
            ChoiceNumber = (int)e.SourceChoice,
            StartDate = DateOnly.FromDateTime(e.ApprovedStartDate.DateTime),
            EndDate = DateOnly.FromDateTime(e.ApprovedEndDate.DateTime)
        });
        
        var dtos = requests.Select(r => MapToDto(r, r.User, r.SubmittedToUser, 
            approvedChoicesMap.GetValueOrDefault(r.Id))).ToList();

        return Ok(dtos);
    }

    [HttpGet("status")]
    [Authorize(Policy = "AnnualLeaveGenerate")]
    public async Task<IActionResult> GetStatus([FromQuery] AnnualLeavePlanLevel level, [FromQuery] Guid sectionId, [FromQuery] Guid? hangarId, [FromQuery] Guid? shopId, [FromQuery] Guid? teamLeaderId, [FromQuery] int year = 0, CancellationToken cancellationToken = default)
    {
        if (year == 0)
        {
            year = DateTime.UtcNow.Year;
        }

        var requestDto = new AnnualLeaveStatusRequestDto
        {
            Level = level,
            SectionId = sectionId,
            HangarId = hangarId,
            ShopId = shopId,
            TeamLeaderId = teamLeaderId,
            Year = year
        };

        var result = await completenessValidator.ValidateCompletenessAsync(requestDto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    [Authorize(Policy = "AnnualLeaveViewTeam")]
    public async Task<IActionResult> GetManpowerSummary([FromQuery] ManpowerSummaryRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await analyticsService.GetManpowerSummaryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("team-leader-analytics")]
    [Authorize(Policy = "AnnualLeaveViewTeam")]
    public async Task<IActionResult> GetTeamLeaderAnalytics([FromQuery] TeamLeaderAnalyticsRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await analyticsService.GetTeamLeaderAnalyticsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("manpower-daily")]
    [Authorize(Policy = "AnnualLeaveViewTeam")]
    public async Task<IActionResult> GetDailyManpowerSummary([FromQuery] ManpowerSummaryRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await analyticsService.GetDailyManpowerSummaryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("balances")]
    [Authorize(Policy = "AnnualLeaveViewTeam")]
    public async Task<IActionResult> GetLeaveBalances([FromQuery] int year, [FromQuery] Guid? sectionId, [FromQuery] Guid? hangarId, CancellationToken cancellationToken = default)
    {
        var result = await analyticsService.GetLeaveBalancesAsync(year, sectionId, hangarId, cancellationToken);
        return Ok(result);
    }

    private RoleAtSubmission DetermineRoleAtSubmission(ApplicationUser user)
    {
        return user.Role switch
        {
            UserRole.Employee => RoleAtSubmission.Employee,
            UserRole.TeamLeader => RoleAtSubmission.TeamLeader,
            UserRole.Manager => RoleAtSubmission.Manager,
            _ => RoleAtSubmission.Employee
        };
    }

    private async Task<ApplicationUser?> DetermineSubmittedToUserAsync(ApplicationUser user, RoleAtSubmission roleAtSubmission, CancellationToken cancellationToken)
    {
        return roleAtSubmission switch
        {
            RoleAtSubmission.Employee => await GetTeamLeaderAsync(user, cancellationToken),
            RoleAtSubmission.TeamLeader => await GetManagerAsync(user, cancellationToken),
            RoleAtSubmission.Manager => await GetDirectorAsync(cancellationToken),
            _ => null
        };
    }

    private async Task<ApplicationUser?> GetTeamLeaderAsync(ApplicationUser employee, CancellationToken cancellationToken)
    {
        // Find Team Leader in same hangar/shop
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => 
                u.Role == UserRole.TeamLeader && 
                u.HangarId == employee.HangarId && 
                u.ShopId == employee.ShopId &&
                u.IsActive, cancellationToken);
    }

    private async Task<ApplicationUser?> GetManagerAsync(ApplicationUser teamLeader, CancellationToken cancellationToken)
    {
        // Find Manager in same section
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => 
                u.Role == UserRole.Manager && 
                u.SectionId == teamLeader.SectionId &&
                u.IsActive, cancellationToken);
    }

    private async Task<ApplicationUser?> GetDirectorAsync(CancellationToken cancellationToken)
    {
        // Find first Director
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Role == UserRole.Director && u.IsActive, cancellationToken);
    }

    private (bool isValid, string errorMessage) ValidateChoicesWithDetails(SubmitAnnualLeaveDto dto, RoleAtSubmission roleAtSubmission)
    {
        // Employees always submit 3 choices
        // Team Leaders and Managers submit 3 choices for Full leave, 6 for Split leave
        var requiredChoices = dto.LeaveType == LeaveType.Split ? 6 : 3;

        if (dto.Choice1StartDate > dto.Choice1EndDate) return (false, "Choice 1: Start date must be before end date");
        if (dto.Choice2StartDate > dto.Choice2EndDate) return (false, "Choice 2: Start date must be before end date");
        if (dto.Choice3StartDate > dto.Choice3EndDate) return (false, "Choice 3: Start date must be before end date");

        if (dto.LeaveType == LeaveType.Split)
        {
            if (!dto.Choice4StartDate.HasValue || !dto.Choice4EndDate.HasValue) return (false, "Choice 4: Both start and end dates are required for split leave");
            if (!dto.Choice5StartDate.HasValue || !dto.Choice5EndDate.HasValue) return (false, "Choice 5: Both start and end dates are required for split leave");
            if (!dto.Choice6StartDate.HasValue || !dto.Choice6EndDate.HasValue) return (false, "Choice 6: Both start and end dates are required for split leave");
            if (dto.Choice4StartDate.Value > dto.Choice4EndDate.Value) return (false, "Choice 4: Start date must be before end date");
            if (dto.Choice5StartDate.Value > dto.Choice5EndDate.Value) return (false, "Choice 5: Start date must be before end date");
            if (dto.Choice6StartDate.Value > dto.Choice6EndDate.Value) return (false, "Choice 6: Start date must be before end date");
        }

        // Check for overlaps between choices 1-3 (applies to all users)
        var choices1to3 = new List<(int choiceNum, DateOnly start, DateOnly end)>
        {
            (1, dto.Choice1StartDate, dto.Choice1EndDate),
            (2, dto.Choice2StartDate, dto.Choice2EndDate),
            (3, dto.Choice3StartDate, dto.Choice3EndDate)
        };

        for (int i = 0; i < choices1to3.Count; i++)
        {
            for (int j = i + 1; j < choices1to3.Count; j++)
            {
                if (choices1to3[i].start <= choices1to3[j].end && choices1to3[i].end >= choices1to3[j].start)
                {
                    return (false, $"Choice {choices1to3[i].choiceNum} and Choice {choices1to3[j].choiceNum} have overlapping date ranges");
                }
            }
        }

        // For split leave, check for overlaps within split groups (choices 1-3 and 4-6 separately)
        if (dto.LeaveType == LeaveType.Split)
        {
            if (dto.Choice4StartDate == null || dto.Choice4EndDate == null ||
                dto.Choice5StartDate == null || dto.Choice5EndDate == null ||
                dto.Choice6StartDate == null || dto.Choice6EndDate == null)
            {
                return (false, "All split leave choices must have start and end dates");
            }

            var choices4to6 = new List<(int choiceNum, DateOnly start, DateOnly end)>
            {
                (4, dto.Choice4StartDate.Value, dto.Choice4EndDate.Value),
                (5, dto.Choice5StartDate.Value, dto.Choice5EndDate.Value),
                (6, dto.Choice6StartDate.Value, dto.Choice6EndDate.Value)
            };

            for (int i = 0; i < choices4to6.Count; i++)
            {
                for (int j = i + 1; j < choices4to6.Count; j++)
                {
                    if (choices4to6[i].start <= choices4to6[j].end && choices4to6[i].end >= choices4to6[j].start)
                    {
                        return (false, $"Choice {choices4to6[i].choiceNum} and Choice {choices4to6[j].choiceNum} have overlapping date ranges");
                    }
                }
            }
        }

        return (true, string.Empty);
    }

    private LeaveChoice CreateChoice(AnnualLeaveRequest request, int choiceNumber, DateOnly startDate, DateOnly endDate, int? splitIndex = null)
    {
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MinValue);
        var days = (endDateTime - startDateTime).Days + 1;

        return new LeaveChoice
        {
            AnnualLeaveRequest = request,
            ChoiceNumber = choiceNumber,
            StartDate = startDateTime,
            EndDate = endDateTime,
            Days = days,
            SplitIndex = splitIndex
        };
    }

    private static AnnualLeaveRequestDto MapToDto(AnnualLeaveRequest request, ApplicationUser user, ApplicationUser submittedToUser, dynamic? approvedChoice = null)
    {
        var choices = request.LeaveChoices.OrderBy(c => c.ChoiceNumber).ToList();
        
        return new AnnualLeaveRequestDto
        {
            Id = request.Id,
            UserId = request.UserId,
            UserName = user.FullName,
            EmployeeId = user.EmployeeId,
            RoleAtSubmission = request.RoleAtSubmission,
            SectionId = request.SectionId,
            SectionName = user.Section?.Name ?? string.Empty,
            HangarId = request.HangarId,
            HangarName = user.Hangar?.Name,
            ShopId = request.ShopId,
            ShopName = user.Shop?.Name,
            SubmittedToUserId = request.SubmittedToUserId,
            SubmittedToUserName = submittedToUser.FullName,
            LeaveType = request.LeaveType,
            Year = request.Year,
            LeaveChoices = choices.Select(c => new LeaveChoiceDto
            {
                Id = c.Id,
                ChoiceNumber = c.ChoiceNumber,
                StartDate = DateOnly.FromDateTime(c.StartDate.DateTime),
                EndDate = DateOnly.FromDateTime(c.EndDate.DateTime),
                Days = c.Days,
                SplitIndex = c.SplitIndex
            }).ToList(),
            Status = request.Status,
            SubmittedAt = request.SubmittedAt ?? DateTimeOffset.MinValue,
            ReviewedAt = request.ReviewedAt,
            RejectionReason = request.RejectionReason,
            CreatedAt = request.CreatedAt,
            // Flat properties for frontend compatibility
            Choice1StartDate = choices.FirstOrDefault(c => c.ChoiceNumber == 1) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 1).StartDate.DateTime) : default,
            Choice1EndDate = choices.FirstOrDefault(c => c.ChoiceNumber == 1) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 1).EndDate.DateTime) : default,
            Choice2StartDate = choices.FirstOrDefault(c => c.ChoiceNumber == 2) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 2).StartDate.DateTime) : default,
            Choice2EndDate = choices.FirstOrDefault(c => c.ChoiceNumber == 2) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 2).EndDate.DateTime) : default,
            Choice3StartDate = choices.FirstOrDefault(c => c.ChoiceNumber == 3) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 3).StartDate.DateTime) : default,
            Choice3EndDate = choices.FirstOrDefault(c => c.ChoiceNumber == 3) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 3).EndDate.DateTime) : default,
            Choice4StartDate = choices.FirstOrDefault(c => c.ChoiceNumber == 4) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 4).StartDate.DateTime) : default,
            Choice4EndDate = choices.FirstOrDefault(c => c.ChoiceNumber == 4) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 4).EndDate.DateTime) : default,
            Choice5StartDate = choices.FirstOrDefault(c => c.ChoiceNumber == 5) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 5).StartDate.DateTime) : default,
            Choice5EndDate = choices.FirstOrDefault(c => c.ChoiceNumber == 5) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 5).EndDate.DateTime) : default,
            Choice6StartDate = choices.FirstOrDefault(c => c.ChoiceNumber == 6) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 6).StartDate.DateTime) : default,
            Choice6EndDate = choices.FirstOrDefault(c => c.ChoiceNumber == 6) != null ? DateOnly.FromDateTime(choices.First(c => c.ChoiceNumber == 6).EndDate.DateTime) : default,
            // Approved choice status after plan generation
            ApprovedChoiceNumber = approvedChoice?.ChoiceNumber,
            ApprovedStartDate = approvedChoice?.StartDate,
            ApprovedEndDate = approvedChoice?.EndDate
        };
    }
}
