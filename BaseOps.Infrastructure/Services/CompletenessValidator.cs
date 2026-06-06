using BaseOps.Application.DTOs;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaseOps.Infrastructure.Services;

public class CompletenessValidator(BaseOpsDbContext dbContext) : ICompletenessValidator
{
    public async Task<AnnualLeaveStatusResponseDto> ValidateCompletenessAsync(AnnualLeaveStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var missingSubmissions = new List<MissingSubmissionDto>();
        var totalRequired = 0;
        var totalSubmitted = 0;

        switch (request.Level)
        {
            case AnnualLeavePlanLevel.TeamLeader:
                (totalRequired, totalSubmitted) = await ValidateTeamLeaderLevelAsync(request, missingSubmissions, cancellationToken);
                break;
            case AnnualLeavePlanLevel.Manager:
                (totalRequired, totalSubmitted) = await ValidateManagerLevelAsync(request, missingSubmissions, cancellationToken);
                break;
            case AnnualLeavePlanLevel.Director:
                (totalRequired, totalSubmitted) = await ValidateDirectorLevelAsync(request, missingSubmissions, cancellationToken);
                break;
        }

        var canGeneratePlan = missingSubmissions.Count == 0;
        var message = canGeneratePlan 
            ? "All required submissions are complete. Plan generation is allowed."
            : $"{missingSubmissions.Count} missing submission(s). Plan generation is not allowed.";

        return new AnnualLeaveStatusResponseDto
        {
            CanGeneratePlan = canGeneratePlan,
            MissingSubmissions = missingSubmissions,
            TotalRequired = totalRequired,
            TotalSubmitted = totalSubmitted,
            Message = message
        };
    }

    private async Task<(int totalRequired, int totalSubmitted)> ValidateTeamLeaderLevelAsync(AnnualLeaveStatusRequestDto request, List<MissingSubmissionDto> missingSubmissions, CancellationToken cancellationToken)
    {
        // Get all employees that report to the Team Leader (via ReportsToUserId)
        var employeesQuery = dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.Employee);

        // Use ReportsToUserId to get employees that report to this team leader
        if (request.TeamLeaderId.HasValue)
        {
            employeesQuery = employeesQuery.Where(u => u.ReportsToUserId == request.TeamLeaderId.Value);
        }
        else
        {
            // Fallback to hangar/shop filtering if TeamLeaderId is not provided
            if (request.HangarId.HasValue)
            {
                employeesQuery = employeesQuery.Where(u => u.HangarId == request.HangarId.Value);
            }
            if (request.ShopId.HasValue)
            {
                employeesQuery = employeesQuery.Where(u => u.ShopId == request.ShopId.Value);
            }
        }

        var employees = await employeesQuery.ToListAsync(cancellationToken);

        var totalRequired = employees.Count;

        // Get submitted leave requests for the year (only from employees that report to this team leader)
        var requestsQuery = dbContext.AnnualLeaveRequests
            .AsNoTracking()
            .Where(r => r.Year == request.Year && r.Status == AnnualLeaveRequestStatus.Submitted)
            .Join(dbContext.Users, r => r.UserId, u => u.Id, (r, u) => new { r, u })
            .Where(x => x.u.Role == UserRole.Employee);

        // Use ReportsToUserId to filter requests from employees that report to this team leader
        if (request.TeamLeaderId.HasValue)
        {
            requestsQuery = requestsQuery.Where(x => x.u.ReportsToUserId == request.TeamLeaderId.Value);
        }
        else
        {
            // Fallback to hangar/shop filtering if TeamLeaderId is not provided
            if (request.HangarId.HasValue)
            {
                requestsQuery = requestsQuery.Where(x => x.r.HangarId == request.HangarId.Value);
            }
            if (request.ShopId.HasValue)
            {
                requestsQuery = requestsQuery.Where(x => x.r.ShopId == request.ShopId.Value);
            }
        }

        var submittedUserIds = await requestsQuery
            .Select(x => x.r.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var totalSubmitted = submittedUserIds.Count;

        // Find missing submissions
        foreach (var employee in employees)
        {
            if (!submittedUserIds.Contains(employee.Id))
            {
                missingSubmissions.Add(new MissingSubmissionDto
                {
                    UserId = employee.Id,
                    UserName = employee.FullName,
                    EmployeeId = employee.EmployeeId,
                    Role = employee.Role.ToString()
                });
            }
        }

        return (totalRequired, totalSubmitted);
    }

    private async Task<(int totalRequired, int totalSubmitted)> ValidateManagerLevelAsync(AnnualLeaveStatusRequestDto request, List<MissingSubmissionDto> missingSubmissions, CancellationToken cancellationToken)
    {
        // Get all Team Leaders in the section
        var teamLeadersQuery = dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.TeamLeader && u.SectionId == request.SectionId);

        if (request.HangarId.HasValue)
        {
            teamLeadersQuery = teamLeadersQuery.Where(u => u.HangarId == request.HangarId.Value);
        }

        var teamLeaders = await teamLeadersQuery.ToListAsync(cancellationToken);
        var totalRequired = teamLeaders.Count;

        // Get submitted leave requests from Team Leaders for the year
        var submittedUserIds = await dbContext.AnnualLeaveRequests
            .AsNoTracking()
            .Where(r => r.Year == request.Year && r.Status == AnnualLeaveRequestStatus.Submitted)
            .Where(r => r.SectionId == request.SectionId)
            .Where(r => r.RoleAtSubmission == RoleAtSubmission.TeamLeader)
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var totalSubmitted = submittedUserIds.Count;

        // Find missing submissions
        foreach (var teamLeader in teamLeaders)
        {
            if (!submittedUserIds.Contains(teamLeader.Id))
            {
                missingSubmissions.Add(new MissingSubmissionDto
                {
                    UserId = teamLeader.Id,
                    UserName = teamLeader.FullName,
                    EmployeeId = teamLeader.EmployeeId,
                    Role = teamLeader.Role.ToString()
                });
            }
        }

        return (totalRequired, totalSubmitted);
    }

    private async Task<(int totalRequired, int totalSubmitted)> ValidateDirectorLevelAsync(AnnualLeaveStatusRequestDto request, List<MissingSubmissionDto> missingSubmissions, CancellationToken cancellationToken)
    {
        // Get all Managers
        var managers = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.Manager)
            .ToListAsync(cancellationToken);

        var totalRequired = managers.Count;

        // Get submitted leave requests from Managers for the year
        var submittedUserIds = await dbContext.AnnualLeaveRequests
            .AsNoTracking()
            .Where(r => r.Year == request.Year && r.Status == AnnualLeaveRequestStatus.Submitted)
            .Where(r => r.RoleAtSubmission == RoleAtSubmission.Manager)
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var totalSubmitted = submittedUserIds.Count;

        // Find missing submissions
        foreach (var manager in managers)
        {
            if (!submittedUserIds.Contains(manager.Id))
            {
                missingSubmissions.Add(new MissingSubmissionDto
                {
                    UserId = manager.Id,
                    UserName = manager.FullName,
                    EmployeeId = manager.EmployeeId,
                    Role = manager.Role.ToString()
                });
            }
        }

        return (totalRequired, totalSubmitted);
    }
}
