using BaseOps.Domain.Entities;

namespace BaseOps.Infrastructure.Services;

public class PriorityCalculator : IPriorityCalculator
{
    // Priority score calculation:
    // - Seniority: Lower EmployeeId = Higher Priority (inverse of EmployeeId numeric value)
    // - Choice ranking: Choice1 > Choice2 > Choice3 (lower choice number = higher priority)
    // - Final score = (1000000 - EmployeeIdNumeric) * 10 + (10 - ChoiceNumber)
    // This ensures seniority is the primary factor, choice is secondary
    
    public int CalculatePriorityScore(AnnualLeaveRequest request, SourceChoice sourceChoice)
    {
        // Extract numeric part from EmployeeId (assuming format like "EMP001", "EMP002", etc.)
        var employeeIdNumeric = ExtractEmployeeIdNumeric(request.User.EmployeeId);
        
        // Seniority score: lower EmployeeId = higher score
        // Use a large base to handle any employee ID range and ensure positive scores
        // Using 1,000,000 as base to handle employee IDs up to 999,999
        var seniorityScore = 1000000 - employeeIdNumeric;
        
        // Choice score: lower choice number = higher score
        var choiceScore = (int)(10 - sourceChoice);
        
        // Combine scores: seniority is primary (multiplied by 10), choice is secondary
        var totalScore = (seniorityScore * 10) + choiceScore;
        
        return totalScore;
    }

    public void SortRequestsByPriority(List<AnnualLeaveRequest> requests)
    {
        // Sort by calculated priority score (descending - higher score = higher priority)
        // For tie-breaking, use EmployeeId as secondary sort (ascending - lower ID first)
        requests.Sort((a, b) =>
        {
            // Calculate scores for Choice1 (primary choice)
            var scoreA = CalculatePriorityScore(a, SourceChoice.Choice1);
            var scoreB = CalculatePriorityScore(b, SourceChoice.Choice1);
            
            // Primary sort by priority score (descending)
            var scoreComparison = scoreB.CompareTo(scoreA);
            if (scoreComparison != 0)
            {
                return scoreComparison;
            }
            
            // Secondary sort by EmployeeId (ascending) for deterministic tie-breaking
            var empIdA = ExtractEmployeeIdNumeric(a.User.EmployeeId);
            var empIdB = ExtractEmployeeIdNumeric(b.User.EmployeeId);
            return empIdA.CompareTo(empIdB);
        });
    }

    private int ExtractEmployeeIdNumeric(string employeeId)
    {
        // Extract numeric part from EmployeeId
        // Handles formats like "EMP001", "EMP-001", "001", "EMP123", etc.
        var numericPart = new string(employeeId.Where(char.IsDigit).ToArray());
        
        if (int.TryParse(numericPart, out var numericValue))
        {
            return numericValue;
        }
        
        // Fallback: use hash of employeeId if no numeric part found
        return Math.Abs(employeeId.GetHashCode()) % 10000;
    }
}
