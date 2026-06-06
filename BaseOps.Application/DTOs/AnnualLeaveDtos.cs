using BaseOps.Domain.Entities;

namespace BaseOps.Application.DTOs;

// Request DTOs
public class SubmitAnnualLeaveDto
{
    public LeaveType? LeaveType { get; set; }
    public DateOnly Choice1StartDate { get; set; }
    public DateOnly Choice1EndDate { get; set; }
    public DateOnly Choice2StartDate { get; set; }
    public DateOnly Choice2EndDate { get; set; }
    public DateOnly Choice3StartDate { get; set; }
    public DateOnly Choice3EndDate { get; set; }
    // Additional choices for split leave
    public DateOnly? Choice4StartDate { get; set; }
    public DateOnly? Choice4EndDate { get; set; }
    public DateOnly? Choice5StartDate { get; set; }
    public DateOnly? Choice5EndDate { get; set; }
    public DateOnly? Choice6StartDate { get; set; }
    public DateOnly? Choice6EndDate { get; set; }
}

public class UpdateAnnualLeaveDto
{
    public LeaveType? LeaveType { get; set; }
    public DateOnly? Choice1StartDate { get; set; }
    public DateOnly? Choice1EndDate { get; set; }
    public DateOnly? Choice2StartDate { get; set; }
    public DateOnly? Choice2EndDate { get; set; }
    public DateOnly? Choice3StartDate { get; set; }
    public DateOnly? Choice3EndDate { get; set; }
}

public class GeneratePlanDto
{
    public AnnualLeavePlanLevel Level { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public Guid? TeamLeaderId { get; set; }
    public int Year { get; set; }
}

public class AdjustPlanEntryDto
{
    public Guid EntryId { get; set; }
    public DateOnly ApprovedStartDate { get; set; }
    public DateOnly ApprovedEndDate { get; set; }
    public string? AdjustmentReason { get; set; }
}

public class AnnualLeaveStatusRequestDto
{
    public AnnualLeavePlanLevel Level { get; set; }
    public Guid SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public Guid? TeamLeaderId { get; set; }
    public int Year { get; set; }
}

public class ManpowerSummaryRequestDto
{
    public int Year { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public Guid? TeamLeaderId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

// Response DTOs
public class AnnualLeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public RoleAtSubmission RoleAtSubmission { get; set; }
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public Guid? HangarId { get; set; }
    public string? HangarName { get; set; }
    public Guid? ShopId { get; set; }
    public string? ShopName { get; set; }
    public Guid SubmittedToUserId { get; set; }
    public string SubmittedToUserName { get; set; } = string.Empty;
    public LeaveType? LeaveType { get; set; }
    public int Year { get; set; }
    public List<LeaveChoiceDto> LeaveChoices { get; set; } = new();
    public AnnualLeaveRequestStatus Status { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    // Flat properties for frontend compatibility
    public DateOnly Choice1StartDate { get; set; }
    public DateOnly Choice1EndDate { get; set; }
    public DateOnly Choice2StartDate { get; set; }
    public DateOnly Choice2EndDate { get; set; }
    public DateOnly Choice3StartDate { get; set; }
    public DateOnly Choice3EndDate { get; set; }
    public DateOnly Choice4StartDate { get; set; }
    public DateOnly Choice4EndDate { get; set; }
    public DateOnly Choice5StartDate { get; set; }
    public DateOnly Choice5EndDate { get; set; }
    public DateOnly Choice6StartDate { get; set; }
    public DateOnly Choice6EndDate { get; set; }
    // Approved choice status after plan generation
    public int? ApprovedChoiceNumber { get; set; }
    public DateOnly? ApprovedStartDate { get; set; }
    public DateOnly? ApprovedEndDate { get; set; }
}

public class LeaveChoiceDto
{
    public Guid Id { get; set; }
    public int ChoiceNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Days { get; set; }
    public int? SplitIndex { get; set; }
}

public class AnnualLeavePlanDto
{
    public Guid Id { get; set; }
    public AnnualLeavePlanLevel Level { get; set; }
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public Guid? HangarId { get; set; }
    public string? HangarName { get; set; }
    public Guid? ShopId { get; set; }
    public string? ShopName { get; set; }
    public Guid? TeamLeaderId { get; set; }
    public string? TeamLeaderName { get; set; }
    public int Year { get; set; }
    public AnnualLeavePlanStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? FinalizedAt { get; set; }
    public List<AnnualLeavePlanEntryDto> Entries { get; set; } = new();
    public int TotalEmployees { get; set; }
    public int TotalOnLeave { get; set; }
    public int TotalAvailable { get; set; }
    public string? GenerationNotes { get; set; }
}

public class AnnualLeavePlanEntryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public DateOnly ApprovedStartDate { get; set; }
    public DateOnly ApprovedEndDate { get; set; }
    public SourceChoice SourceChoice { get; set; }
    public int PriorityScore { get; set; }
    public bool IsManuallyAdjusted { get; set; }
    public DateTimeOffset? ManuallyAdjustedAt { get; set; }
    public string? ManuallyAdjustedByUserName { get; set; }
    public string? AdjustmentReason { get; set; }
    public int? SplitIndex { get; set; }
    public string? UserRole { get; set; }
}

public class AnnualLeaveStatusResponseDto
{
    public bool CanGeneratePlan { get; set; }
    public List<MissingSubmissionDto> MissingSubmissions { get; set; } = new();
    public int TotalRequired { get; set; }
    public int TotalSubmitted { get; set; }
    public string? Message { get; set; }
}

public class MissingSubmissionDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class ManpowerSummaryResponseDto
{
    public int Year { get; set; }
    public string Scope { get; set; } = string.Empty;
    public List<MonthlyManpowerSummaryDto> MonthlySummary { get; set; } = new();
    public List<CriticalPeriodDto> CriticalPeriods { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class MonthlyManpowerSummaryDto
{
    public string Month { get; set; } = string.Empty;
    public decimal AverageManpower { get; set; }
    public decimal LowestManpower { get; set; }
    public int CriticalDays { get; set; }
}

public class CriticalPeriodDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int Shortage { get; set; }
}

public class LeaveBalanceDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public int Year { get; set; }
    public int TotalEntitled { get; set; }
    public int Taken { get; set; }
    public int Pending { get; set; }
    public int Remaining { get; set; }
    public int CarryOverFromPrevious { get; set; }
    public int CarryOverToNext { get; set; }
}

public class DailyManpowerSummaryDto
{
    public DateOnly Date { get; set; }
    public int TotalEmployees { get; set; }
    public int OnLeave { get; set; }
    public int Available { get; set; }
    public int? Shortage { get; set; }
    public bool IsCritical { get; set; }
}

// Team Leader Operational Analytics DTOs
public class TeamLeaderAnalyticsRequestDto
{
    public int Year { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public TeamLeaderAnalyticsRulesDto? Rules { get; set; }
}

public class TeamLeaderAnalyticsResponseDto
{
    public int Year { get; set; }
    public string Scope { get; set; } = string.Empty;
    public TeamLeaderSummaryMetricsDto SummaryMetrics { get; set; } = new();
    public List<TeamLeaderMonthlyTrendDto> MonthlyTrends { get; set; } = new();
    public List<TeamLeaderCriticalPeriodDto> CriticalPeriods { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

public class TeamLeaderSummaryMetricsDto
{
    // Core Metrics
    public int TotalLeaveDays { get; set; }
    public decimal AverageLeadershipAvailability { get; set; }
    public string PeakLeaveDay { get; set; } = string.Empty;
    public int PeakConcurrentTeamLeadersOnLeave { get; set; }
    public int LowestSupervisoryCapacity { get; set; }
    public int CriticalOperationalDays { get; set; }

    // Operational Metrics
    public decimal LeadershipCoverageRatio { get; set; }
    public decimal SupervisionStabilityIndex { get; set; }
    public decimal OperationalRiskScore { get; set; }
    public decimal LeadershipAvailabilityPercentage { get; set; }
    public decimal ComplianceRiskIndicator { get; set; }
    public decimal MaintenanceReadinessScore { get; set; }

    // Risk Indicators
    public string OverallRiskLevel { get; set; } = string.Empty; // Green, Yellow, Orange, Red
}

public class TeamLeaderMonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int MonthNumber { get; set; }
    
    // Leadership Leave Trend
    public int TotalTeamLeaderLeaveDays { get; set; }
    public decimal AverageConcurrentLeave { get; set; }
    public int PeakConcurrentLeave { get; set; }
    
    // Operational Stability Trend
    public decimal MonthlySupervisionCapacity { get; set; }
    public decimal LeadershipAvailabilityTrend { get; set; }
    public decimal ShiftRiskFluctuation { get; set; }
    public decimal OperationalReadinessTrend { get; set; }
    
    // Risk Trend Analytics
    public int ShortageDays { get; set; }
    public decimal LeadershipPressureIndex { get; set; }
    public bool HasRecurringCriticalPeriods { get; set; }
    public string RiskTrend { get; set; } = string.Empty; // Increasing, Stable, Decreasing
}

public class TeamLeaderCriticalPeriodDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string RootCause { get; set; } = string.Empty;
    public string AffectedHangarShop { get; set; } = string.Empty;
    public string AffectedShifts { get; set; } = string.Empty;
    public string OperationalImpact { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public int TeamLeadersOnLeave { get; set; }
    public int RemainingSupervisors { get; set; }
    public decimal OperationalRiskScore { get; set; }
}

// Configurable Rules DTO
public class TeamLeaderAnalyticsRulesDto
{
    public int MinimumTeamLeadersRequired { get; set; } = 2;
    public int MaxConcurrentLeavePerArea { get; set; } = 2;
    public int MinimumShiftCoverage { get; set; } = 1;
    public decimal CriticalCoverageThreshold { get; set; } = 0.5m; // 50%
    public decimal HighRiskThreshold { get; set; } = 0.7m; // 70%
    public decimal ModerateRiskThreshold { get; set; } = 0.8m; // 80%
}
