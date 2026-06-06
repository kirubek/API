using BaseOps.Application.DTOs;
using BaseOps.Domain.Entities;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaseOps.Infrastructure.Services;

public class AllocationEngine(
    BaseOpsDbContext dbContext,
    IPriorityCalculator priorityCalculator,
    IManpowerConstraintService manpowerConstraintService) : IAllocationEngine
{
    public async Task<List<AnnualLeavePlanEntry>> GeneratePlanEntriesAsync(
        List<AnnualLeaveRequest> requests,
        GeneratePlanDto planDto,
        CancellationToken cancellationToken = default)
    {
        var planEntries = new List<AnnualLeavePlanEntry>();
        var currentAllocations = new Dictionary<DateOnly, HashSet<Guid>>(); // Date -> UserIds on leave

        // Sort requests by priority
        priorityCalculator.SortRequestsByPriority(requests);

        // Get manpower constraints for the scope
        var constraints = await manpowerConstraintService.GetConstraintsAsync(
            planDto.SectionId ?? Guid.Empty,
            planDto.HangarId,
            planDto.ShopId,
            planDto.Year,
            cancellationToken);

        // Process each request in priority order
        foreach (var request in requests)
        {
            if (request.LeaveType == LeaveType.Split)
            {
                // For split leave, process two separate allocations:
                // Group 1: Choices 1-3 (SplitIndex = 1)
                // Group 2: Choices 4-6 (SplitIndex = 2)
                var splitAllocations = new List<(SourceChoice choice, DateOnly start, DateOnly end, int splitIndex)>();
                
                // Process first split group (choices 1-3)
                for (int choiceNum = 1; choiceNum <= 3; choiceNum++)
                {
                    var choice = request.LeaveChoices.FirstOrDefault(c => c.ChoiceNumber == choiceNum);
                    if (choice == null) continue;

                    if (await CanAllocateChoiceAsync(choice, currentAllocations, constraints, cancellationToken))
                    {
                        var startDate = DateOnly.FromDateTime(choice.StartDate.DateTime);
                        var endDate = DateOnly.FromDateTime(choice.EndDate.DateTime);
                        splitAllocations.Add(((SourceChoice)choiceNum, startDate, endDate, 1));
                        
                        // Add to current allocations
                        for (var date = startDate; date <= endDate; date = date.AddDays(1))
                        {
                            if (!currentAllocations.ContainsKey(date))
                            {
                                currentAllocations[date] = new HashSet<Guid>();
                            }
                            currentAllocations[date].Add(request.UserId);
                        }
                        break;
                    }
                }
                
                // Process second split group (choices 4-6)
                for (int choiceNum = 4; choiceNum <= 6; choiceNum++)
                {
                    var choice = request.LeaveChoices.FirstOrDefault(c => c.ChoiceNumber == choiceNum);
                    if (choice == null) continue;

                    if (await CanAllocateChoiceAsync(choice, currentAllocations, constraints, cancellationToken))
                    {
                        var startDate = DateOnly.FromDateTime(choice.StartDate.DateTime);
                        var endDate = DateOnly.FromDateTime(choice.EndDate.DateTime);
                        splitAllocations.Add(((SourceChoice)choiceNum, startDate, endDate, 2));
                        
                        // Add to current allocations
                        for (var date = startDate; date <= endDate; date = date.AddDays(1))
                        {
                            if (!currentAllocations.ContainsKey(date))
                            {
                                currentAllocations[date] = new HashSet<Guid>();
                            }
                            currentAllocations[date].Add(request.UserId);
                        }
                        break;
                    }
                }
                
                // Create plan entries for each allocated split
                foreach (var (choice, start, end, splitIndex) in splitAllocations)
                {
                    var entry = new AnnualLeavePlanEntry
                    {
                        Id = Guid.NewGuid(),
                        AnnualLeavePlanId = Guid.Empty, // Will be set when plan is created
                        UserId = request.UserId,
                        AnnualLeaveRequestId = request.Id,
                        ApprovedStartDate = start.ToDateTime(TimeOnly.MinValue),
                        ApprovedEndDate = end.ToDateTime(TimeOnly.MinValue),
                        SourceChoice = choice,
                        PriorityScore = priorityCalculator.CalculatePriorityScore(request, choice),
                        IsManuallyAdjusted = false,
                        SplitIndex = splitIndex
                    };
                    planEntries.Add(entry);
                }
                
                // If no allocations were made, skip this request (no placeholder entry)
                // The user will need to be manually addressed or constraints adjusted
            }
            else
            {
                // For full leave, process choices 1-3 normally
                var allocated = false;
                SourceChoice? usedChoice = null;
                DateOnly? allocatedStart = null;
                DateOnly? allocatedEnd = null;

                for (int choiceNum = 1; choiceNum <= 3; choiceNum++)
                {
                    var choice = request.LeaveChoices.FirstOrDefault(c => c.ChoiceNumber == choiceNum);
                    if (choice == null) continue;

                    if (await CanAllocateChoiceAsync(choice, currentAllocations, constraints, cancellationToken))
                    {
                        allocated = true;
                        usedChoice = (SourceChoice)choiceNum;
                        allocatedStart = DateOnly.FromDateTime(choice.StartDate.DateTime);
                        allocatedEnd = DateOnly.FromDateTime(choice.EndDate.DateTime);

                        for (var date = allocatedStart.Value; date <= allocatedEnd.Value; date = date.AddDays(1))
                        {
                            if (!currentAllocations.ContainsKey(date))
                            {
                                currentAllocations[date] = new HashSet<Guid>();
                            }
                            currentAllocations[date].Add(request.UserId);
                        }

                        break;
                    }
                }

                // Only create an entry if allocation was successful
                if (allocated)
                {
                    var entry = new AnnualLeavePlanEntry
                    {
                        Id = Guid.NewGuid(),
                        AnnualLeavePlanId = Guid.Empty,
                        UserId = request.UserId,
                        AnnualLeaveRequestId = request.Id,
                        ApprovedStartDate = allocatedStart.Value.ToDateTime(TimeOnly.MinValue),
                        ApprovedEndDate = allocatedEnd.Value.ToDateTime(TimeOnly.MinValue),
                        SourceChoice = usedChoice.Value,
                        PriorityScore = priorityCalculator.CalculatePriorityScore(request, usedChoice.Value),
                        IsManuallyAdjusted = false,
                        SplitIndex = null
                    };
                    planEntries.Add(entry);
                }
                // If not allocated, skip this request (no placeholder entry)
            }
        }

        return planEntries;
    }

    private async Task<bool> CanAllocateChoiceAsync(
        LeaveChoice choice,
        Dictionary<DateOnly, HashSet<Guid>> currentAllocations,
        ManpowerConstraint constraints,
        CancellationToken cancellationToken)
    {
        var startDate = DateOnly.FromDateTime(choice.StartDate.DateTime);
        var endDate = DateOnly.FromDateTime(choice.EndDate.DateTime);

        // Check each date in the choice range
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Get current count for this date
            var currentCount = currentAllocations.ContainsKey(date) ? currentAllocations[date].Count : 0;

            // Check against constraints
            var maxAllowed = constraints.MaxLeaveCount ?? 
                (int)Math.Ceiling(await GetTotalEmployeesAsync(constraints, null, cancellationToken) * constraints.MaxLeavePercentage);
            
            var minRequired = constraints.MinCoverageCount ??
                (int)Math.Ceiling(await GetTotalEmployeesAsync(constraints, null, cancellationToken) * constraints.MinCoveragePercentage);

            // Check if adding this user would exceed max leave
            if (currentCount + 1 > maxAllowed)
            {
                return false;
            }

            // Check if this would leave below minimum coverage
            var totalEmployees = await GetTotalEmployeesAsync(constraints, null, cancellationToken);
            if (totalEmployees - (currentCount + 1) < minRequired)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<int> GetTotalEmployeesAsync(ManpowerConstraint constraints, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsNoTracking().Where(u => u.IsActive);

        if (constraints.SectionId != Guid.Empty)
        {
            query = query.Where(u => u.SectionId == constraints.SectionId);
        }
        if (constraints.HangarId.HasValue)
        {
            query = query.Where(u => u.HangarId == constraints.HangarId.Value);
        }
        if (constraints.ShopId.HasValue)
        {
            query = query.Where(u => u.ShopId == constraints.ShopId.Value);
        }

        // Exclude the specified user (e.g., team leader) from the count
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }
}
