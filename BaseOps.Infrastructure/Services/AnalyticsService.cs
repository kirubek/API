using BaseOps.Application.DTOs;
using BaseOps.Domain.Entities;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaseOps.Infrastructure.Services;

public class AnalyticsService(BaseOpsDbContext dbContext) : IAnalyticsService
{
    public async Task<ManpowerSummaryResponseDto> GetManpowerSummaryAsync(ManpowerSummaryRequestDto request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate ?? new DateOnly(request.Year, 1, 1);
        var endDate = request.EndDate ?? new DateOnly(request.Year, 12, 31);

        // Get finalized and draft plans for the scope (allow pre-finalization analytics)
        var plansQuery = dbContext.AnnualLeavePlans
            .Include(p => p.Entries)
            .AsNoTracking()
            .Where(p => p.Year == request.Year && (p.Status == AnnualLeavePlanStatus.Finalized || p.Status == AnnualLeavePlanStatus.Draft));

        if (request.SectionId.HasValue)
        {
            plansQuery = plansQuery.Where(p => p.SectionId == request.SectionId.Value);
        }
        if (request.HangarId.HasValue)
        {
            plansQuery = plansQuery.Where(p => p.HangarId == request.HangarId.Value);
        }
        if (request.TeamLeaderId.HasValue)
        {
            plansQuery = plansQuery.Where(p => p.TeamLeaderId == request.TeamLeaderId.Value);
        }

        var plans = await plansQuery.ToListAsync(cancellationToken);

        // Recalculate total employees dynamically based on scope (excluding team leader for TeamLeader level)
        var totalEmployeesQuery = dbContext.Users.AsNoTracking().Where(u => u.IsActive);
        if (request.SectionId.HasValue)
        {
            totalEmployeesQuery = totalEmployeesQuery.Where(u => u.SectionId == request.SectionId.Value);
        }
        if (request.HangarId.HasValue)
        {
            totalEmployeesQuery = totalEmployeesQuery.Where(u => u.HangarId == request.HangarId.Value);
        }
        if (request.TeamLeaderId.HasValue)
        {
            // Exclude the team leader from the count
            totalEmployeesQuery = totalEmployeesQuery.Where(u => u.Id != request.TeamLeaderId.Value);
        }
        var totalEmployees = await totalEmployeesQuery.CountAsync(cancellationToken);

        // Calculate monthly summaries
        var monthlySummaries = new List<MonthlyManpowerSummaryDto>();
        var criticalPeriods = new List<CriticalPeriodDto>();

        for (int month = 1; month <= 12; month++)
        {
            var monthStart = new DateOnly(request.Year, month, 1);
            var monthEnd = new DateOnly(request.Year, month, DateTime.DaysInMonth(request.Year, month));

            var monthlyEntries = plans
                .SelectMany(p => p.Entries)
                .Where(e => e.ApprovedStartDate > DateTimeOffset.MinValue)
                .Where(e => DateOnly.FromDateTime(e.ApprovedStartDate.DateTime) >= monthStart && 
                           DateOnly.FromDateTime(e.ApprovedStartDate.DateTime) <= monthEnd)
                .ToList();

            var onLeave = monthlyEntries.Count;
            var available = totalEmployees - onLeave;
            var averageManpower = totalEmployees > 0 ? (decimal)available / totalEmployees : 0;
            var lowestManpower = averageManpower; // Simplified for now

            // Check for critical periods
            var shortage = totalEmployees - available;
            if (shortage > 0)
            {
                criticalPeriods.Add(new CriticalPeriodDto
                {
                    StartDate = monthStart,
                    EndDate = monthEnd,
                    Reason = "High leave demand",
                    Shortage = shortage
                });
            }

            monthlySummaries.Add(new MonthlyManpowerSummaryDto
            {
                Month = monthStart.ToString("MMMM yyyy"),
                AverageManpower = averageManpower,
                LowestManpower = lowestManpower,
                CriticalDays = shortage > 0 ? DateTime.DaysInMonth(request.Year, month) : 0
            });
        }

        // Generate recommendations
        var recommendations = new List<string>();
        if (criticalPeriods.Any())
        {
            recommendations.Add($"Critical manpower shortages detected in {criticalPeriods.Count} periods. Consider rescheduling leave requests.");
        }
        if (monthlySummaries.Any(m => m.CriticalDays > 5))
        {
            recommendations.Add("Extended critical periods detected. Review leave distribution across the year.");
        }

        return new ManpowerSummaryResponseDto
        {
            Year = request.Year,
            Scope = await DetermineScopeAsync(request, dbContext, cancellationToken),
            MonthlySummary = monthlySummaries,
            CriticalPeriods = criticalPeriods,
            Recommendations = recommendations
        };
    }

    public async Task<List<DailyManpowerSummaryDto>> GetDailyManpowerSummaryAsync(ManpowerSummaryRequestDto request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate ?? new DateOnly(request.Year, 1, 1);
        var endDate = request.EndDate ?? new DateOnly(request.Year, 12, 31);

        // Get finalized and draft plans for the scope (allow pre-finalization analytics)
        var plansQuery = dbContext.AnnualLeavePlans
            .Include(p => p.Entries)
            .AsNoTracking()
            .Where(p => p.Year == request.Year && (p.Status == AnnualLeavePlanStatus.Finalized || p.Status == AnnualLeavePlanStatus.Draft));

        if (request.SectionId.HasValue)
        {
            plansQuery = plansQuery.Where(p => p.SectionId == request.SectionId.Value);
        }
        if (request.HangarId.HasValue)
        {
            plansQuery = plansQuery.Where(p => p.HangarId == request.HangarId.Value);
        }
        if (request.TeamLeaderId.HasValue)
        {
            plansQuery = plansQuery.Where(p => p.TeamLeaderId == request.TeamLeaderId.Value);
        }

        var plans = await plansQuery.ToListAsync(cancellationToken);

        // Recalculate total employees dynamically based on scope (excluding team leader for TeamLeader level)
        var totalEmployeesQuery = dbContext.Users.AsNoTracking().Where(u => u.IsActive);
        if (request.SectionId.HasValue)
        {
            totalEmployeesQuery = totalEmployeesQuery.Where(u => u.SectionId == request.SectionId.Value);
        }
        if (request.HangarId.HasValue)
        {
            totalEmployeesQuery = totalEmployeesQuery.Where(u => u.HangarId == request.HangarId.Value);
        }
        if (request.TeamLeaderId.HasValue)
        {
            // Exclude the team leader from the count
            totalEmployeesQuery = totalEmployeesQuery.Where(u => u.Id != request.TeamLeaderId.Value);
        }
        var totalEmployees = await totalEmployeesQuery.CountAsync(cancellationToken);

        var dailySummaries = new List<DailyManpowerSummaryDto>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            var entriesOnDate = plans
                .SelectMany(p => p.Entries)
                .Where(e => e.ApprovedStartDate > DateTimeOffset.MinValue)
                .Where(e => DateOnly.FromDateTime(e.ApprovedStartDate.DateTime) <= currentDate &&
                           DateOnly.FromDateTime(e.ApprovedEndDate.DateTime) >= currentDate)
                .ToList();

            var onLeave = entriesOnDate.Count;
            var available = totalEmployees - onLeave;
            int? shortage = null;
            if (available < (totalEmployees * 0.6m))
            {
                shortage = (int)((totalEmployees * 0.6m) - available);
            }
            var isCritical = shortage.HasValue && shortage.Value > 0;

            dailySummaries.Add(new DailyManpowerSummaryDto
            {
                Date = currentDate,
                TotalEmployees = totalEmployees,
                OnLeave = onLeave,
                Available = available,
                Shortage = shortage,
                IsCritical = isCritical
            });

            currentDate = currentDate.AddDays(1);
        }

        return dailySummaries;
    }

    public async Task<List<LeaveBalanceDto>> GetLeaveBalancesAsync(int year, Guid? sectionId, Guid? hangarId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.LeaveBalances
            .Include(lb => lb.User)
            .AsNoTracking()
            .Where(lb => lb.Year == year);

        if (sectionId.HasValue)
        {
            query = query.Where(lb => lb.User.SectionId == sectionId.Value);
        }
        if (hangarId.HasValue)
        {
            query = query.Where(lb => lb.User.HangarId == hangarId.Value);
        }

        var balances = await query.ToListAsync(cancellationToken);

        return balances.Select(lb => new LeaveBalanceDto
        {
            UserId = lb.UserId,
            UserName = lb.User.FullName,
            EmployeeId = lb.User.EmployeeId,
            Year = lb.Year,
            TotalEntitled = lb.TotalEntitled,
            Taken = lb.Taken,
            Pending = lb.Pending,
            Remaining = lb.Remaining,
            CarryOverFromPrevious = lb.CarryOverFromPrevious,
            CarryOverToNext = lb.CarryOverToNext
        }).ToList();
    }

    private static async Task<string> DetermineScopeAsync(ManpowerSummaryRequestDto request, BaseOpsDbContext dbContext, CancellationToken cancellationToken)
    {
        if (request.TeamLeaderId.HasValue)
        {
            var teamLeader = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == request.TeamLeaderId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
            return $"Team Leader: {teamLeader ?? "Unknown"}";
        }
        if (request.HangarId.HasValue)
        {
            var hangar = await dbContext.Hangars
                .AsNoTracking()
                .Where(h => h.Id == request.HangarId.Value)
                .Select(h => h.Name)
                .FirstOrDefaultAsync(cancellationToken);
            return $"Hangar: {hangar ?? "Unknown"}";
        }
        if (request.SectionId.HasValue)
        {
            var section = await dbContext.Sections
                .AsNoTracking()
                .Where(s => s.Id == request.SectionId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
            return $"Section: {section ?? "Unknown"}";
        }
        return "Enterprise";
    }

    public async Task<TeamLeaderAnalyticsResponseDto> GetTeamLeaderAnalyticsAsync(TeamLeaderAnalyticsRequestDto request, CancellationToken cancellationToken = default)
    {
        // Use default rules if not provided
        var rules = request.Rules ?? new TeamLeaderAnalyticsRulesDto();
        
        // Get all Team Leaders in the scope
        var teamLeadersQuery = dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == BaseOps.Domain.Enums.UserRole.TeamLeader);
        
        if (request.SectionId.HasValue)
            teamLeadersQuery = teamLeadersQuery.Where(u => u.SectionId == request.SectionId.Value);
        if (request.HangarId.HasValue)
            teamLeadersQuery = teamLeadersQuery.Where(u => u.HangarId == request.HangarId.Value);
        if (request.ShopId.HasValue)
            teamLeadersQuery = teamLeadersQuery.Where(u => u.ShopId == request.ShopId.Value);
        
        var teamLeaders = await teamLeadersQuery.ToListAsync(cancellationToken);
        var totalTeamLeaders = teamLeaders.Count;
        
        // Get finalized plans for Team Leaders in the scope
        var plansQuery = dbContext.AnnualLeavePlans
            .Include(p => p.Entries)
            .AsNoTracking()
            .Where(p => p.Year == request.Year && p.Status == AnnualLeavePlanStatus.Finalized);
        
        if (request.SectionId.HasValue)
            plansQuery = plansQuery.Where(p => p.SectionId == request.SectionId.Value);
        if (request.HangarId.HasValue)
            plansQuery = plansQuery.Where(p => p.HangarId == request.HangarId.Value);
        if (request.ShopId.HasValue)
            plansQuery = plansQuery.Where(p => p.ShopId == request.ShopId.Value);
        
        var plans = await plansQuery.ToListAsync(cancellationToken);
        
        // Calculate daily Team Leader availability
        var startDate = new DateOnly(request.Year, 1, 1);
        var endDate = new DateOnly(request.Year, 12, 31);
        var dailyAvailability = CalculateDailyTeamLeaderAvailability(startDate, endDate, plans, teamLeaders.Select(tl => tl.Id).ToList());
        
        // Calculate summary metrics
        var summaryMetrics = CalculateSummaryMetrics(dailyAvailability, totalTeamLeaders, rules);
        
        // Calculate monthly trends
        var monthlyTrends = CalculateMonthlyTrends(dailyAvailability, totalTeamLeaders, request.Year, rules);
        
        // Identify critical periods
        var criticalPeriods = IdentifyCriticalPeriods(dailyAvailability, totalTeamLeaders, rules, teamLeaders);
        
        // Generate insights
        var insights = GenerateInsights(summaryMetrics, monthlyTrends, criticalPeriods, teamLeaders);
        
        // Determine scope
        var scope = await GetScopeNameAsync(request.SectionId, request.HangarId, request.ShopId, cancellationToken);
        
        return new TeamLeaderAnalyticsResponseDto
        {
            Year = request.Year,
            Scope = scope,
            SummaryMetrics = summaryMetrics,
            MonthlyTrends = monthlyTrends,
            CriticalPeriods = criticalPeriods,
            Insights = insights
        };
    }
    
    private Dictionary<DateOnly, (int OnLeave, int Available)> CalculateDailyTeamLeaderAvailability(
        DateOnly startDate, DateOnly endDate, List<AnnualLeavePlan> plans, List<Guid> teamLeaderIds)
    {
        var dailyAvailability = new Dictionary<DateOnly, (int OnLeave, int Available)>();
        
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var onLeave = 0;
            
            foreach (var plan in plans)
            {
                foreach (var entry in plan.Entries)
                {
                    if (!teamLeaderIds.Contains(entry.UserId)) continue;
                    
                    var entryStart = DateOnly.FromDateTime(entry.ApprovedStartDate.DateTime);
                    var entryEnd = DateOnly.FromDateTime(entry.ApprovedEndDate.DateTime);
                    
                    if (date >= entryStart && date <= entryEnd)
                    {
                        onLeave++;
                    }
                }
            }
            
            dailyAvailability[date] = (onLeave, teamLeaderIds.Count - onLeave);
        }
        
        return dailyAvailability;
    }
    
    private TeamLeaderSummaryMetricsDto CalculateSummaryMetrics(
        Dictionary<DateOnly, (int OnLeave, int Available)> dailyAvailability, 
        int totalTeamLeaders, 
        TeamLeaderAnalyticsRulesDto rules)
    {
        if (totalTeamLeaders == 0)
        {
            return new TeamLeaderSummaryMetricsDto
            {
                OverallRiskLevel = "Green"
            };
        }
        
        var totalLeaveDays = dailyAvailability.Values.Sum(v => v.OnLeave);
        var averageLeadershipAvailability = dailyAvailability.Values.Average(v => (decimal)v.Available / totalTeamLeaders);
        
        var peakDay = dailyAvailability.OrderByDescending(d => d.Value.OnLeave).FirstOrDefault();
        var peakConcurrentLeave = peakDay.Value.OnLeave;
        var peakLeaveDay = peakDay.Key.ToString("dd MMM yyyy");
        
        var lowestCapacity = dailyAvailability.Min(d => d.Value.Available);
        var criticalDays = dailyAvailability.Count(d => d.Value.Available < rules.MinimumTeamLeadersRequired);
        
        // Leadership Coverage Ratio: Available / Total
        var leadershipCoverageRatio = totalTeamLeaders > 0 
            ? (decimal)dailyAvailability.Average(d => (decimal)d.Value.Available / totalTeamLeaders) 
            : 0;
        
        // Supervision Stability Index: inverse of variance in availability
        var availabilityValues = dailyAvailability.Values.Select(v => (decimal)v.Available / totalTeamLeaders).ToList();
        var variance = availabilityValues.Any() ? availabilityValues.Average(v => Math.Pow((double)(v - availabilityValues.Average()), 2)) : 0;
        var supervisionStabilityIndex = 1 - (decimal)Math.Sqrt(variance);
        
        // Operational Risk Score: weighted combination of factors
        var operationalRiskScore = CalculateOperationalRiskScore(
            peakConcurrentLeave, 
            totalTeamLeaders, 
            criticalDays, 
            dailyAvailability.Count,
            rules);
        
        // Leadership Availability Percentage
        var leadershipAvailabilityPercentage = averageLeadershipAvailability * 100;
        
        // Compliance Risk Indicator: based on days below minimum
        var complianceRiskIndicator = dailyAvailability.Count > 0 
            ? (decimal)criticalDays / dailyAvailability.Count 
            : 0;
        
        // Maintenance Readiness Score: inverse of risk
        var maintenanceReadinessScore = 1 - operationalRiskScore;
        
        // Overall Risk Level
        var overallRiskLevel = DetermineRiskLevel(operationalRiskScore, rules);
        
        return new TeamLeaderSummaryMetricsDto
        {
            TotalLeaveDays = totalLeaveDays,
            AverageLeadershipAvailability = averageLeadershipAvailability,
            PeakLeaveDay = peakLeaveDay,
            PeakConcurrentTeamLeadersOnLeave = peakConcurrentLeave,
            LowestSupervisoryCapacity = lowestCapacity,
            CriticalOperationalDays = criticalDays,
            LeadershipCoverageRatio = leadershipCoverageRatio,
            SupervisionStabilityIndex = supervisionStabilityIndex,
            OperationalRiskScore = operationalRiskScore,
            LeadershipAvailabilityPercentage = leadershipAvailabilityPercentage,
            ComplianceRiskIndicator = complianceRiskIndicator,
            MaintenanceReadinessScore = maintenanceReadinessScore,
            OverallRiskLevel = overallRiskLevel
        };
    }
    
    private decimal CalculateOperationalRiskScore(
        int peakConcurrentLeave, 
        int totalTeamLeaders, 
        int criticalDays, 
        int totalDays,
        TeamLeaderAnalyticsRulesDto rules)
    {
        // Weighted risk factors
        var concurrentLeaveRisk = totalTeamLeaders > 0 
            ? (decimal)peakConcurrentLeave / totalTeamLeaders 
            : 0;
        
        var criticalDayRisk = totalDays > 0 
            ? (decimal)criticalDays / totalDays 
            : 0;
        
        // Combined weighted score (adjust weights as needed)
        var riskScore = (concurrentLeaveRisk * 0.6m) + (criticalDayRisk * 0.4m);
        
        return Math.Min(riskScore, 1); // Cap at 1
    }
    
    private string DetermineRiskLevel(decimal riskScore, TeamLeaderAnalyticsRulesDto rules)
    {
        if (riskScore >= 1 - rules.CriticalCoverageThreshold) return "Red";
        if (riskScore >= 1 - rules.HighRiskThreshold) return "Orange";
        if (riskScore >= 1 - rules.ModerateRiskThreshold) return "Yellow";
        return "Green";
    }
    
    private List<TeamLeaderMonthlyTrendDto> CalculateMonthlyTrends(
        Dictionary<DateOnly, (int OnLeave, int Available)> dailyAvailability,
        int totalTeamLeaders,
        int year,
        TeamLeaderAnalyticsRulesDto rules)
    {
        var monthlyTrends = new List<TeamLeaderMonthlyTrendDto>();
        
        for (int month = 1; month <= 12; month++)
        {
            var monthStart = new DateOnly(year, month, 1);
            var monthEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            
            var monthDays = dailyAvailability
                .Where(d => d.Key >= monthStart && d.Key <= monthEnd)
                .ToList();
            
            if (!monthDays.Any()) continue;
            
            var totalTeamLeaderLeaveDays = monthDays.Sum(d => d.Value.OnLeave);
            var averageConcurrentLeave = monthDays.Average(d => d.Value.OnLeave);
            var peakConcurrentLeave = monthDays.Max(d => d.Value.OnLeave);
            
            var monthlySupervisionCapacity = totalTeamLeaders > 0 
                ? (decimal)monthDays.Average(d => d.Value.Available) / totalTeamLeaders 
                : 0;
            
            var leadershipAvailabilityTrend = monthlySupervisionCapacity;
            var shiftRiskFluctuation = monthDays.Max(d => (decimal)d.Value.OnLeave / totalTeamLeaders) - 
                                        monthDays.Min(d => (decimal)d.Value.OnLeave / totalTeamLeaders);
            
            var operationalReadinessTrend = 1 - shiftRiskFluctuation;
            
            var shortageDays = monthDays.Count(d => d.Value.Available < rules.MinimumTeamLeadersRequired);
            
            var leadershipPressureIndex = totalTeamLeaders > 0 
                ? (decimal)peakConcurrentLeave / totalTeamLeaders 
                : 0;
            
            // Determine risk trend based on comparison with previous month
            var riskTrend = "Stable";
            if (month > 1)
            {
                var prevMonthStart = new DateOnly(year, month - 1, 1);
                var prevMonthEnd = new DateOnly(year, month - 1, DateTime.DaysInMonth(year, month - 1));
                var prevMonthDays = dailyAvailability
                    .Where(d => d.Key >= prevMonthStart && d.Key <= prevMonthEnd)
                    .ToList();
                
                if (prevMonthDays.Any())
                {
                    var prevAvgLeave = prevMonthDays.Average(d => d.Value.OnLeave);
                    var currentAvgLeave = monthDays.Average(d => d.Value.OnLeave);
                    
                    if (currentAvgLeave > prevAvgLeave * 1.1) riskTrend = "Increasing";
                    else if (currentAvgLeave < prevAvgLeave * 0.9) riskTrend = "Decreasing";
                }
            }
            
            monthlyTrends.Add(new TeamLeaderMonthlyTrendDto
            {
                Month = monthStart.ToString("MMMM"),
                MonthNumber = month,
                TotalTeamLeaderLeaveDays = totalTeamLeaderLeaveDays,
                AverageConcurrentLeave = (decimal)averageConcurrentLeave,
                PeakConcurrentLeave = peakConcurrentLeave,
                MonthlySupervisionCapacity = monthlySupervisionCapacity,
                LeadershipAvailabilityTrend = leadershipAvailabilityTrend,
                ShiftRiskFluctuation = shiftRiskFluctuation,
                OperationalReadinessTrend = operationalReadinessTrend,
                ShortageDays = shortageDays,
                LeadershipPressureIndex = leadershipPressureIndex,
                HasRecurringCriticalPeriods = shortageDays > 5, // Threshold for "recurring"
                RiskTrend = riskTrend
            });
        }
        
        return monthlyTrends;
    }
    
    private List<TeamLeaderCriticalPeriodDto> IdentifyCriticalPeriods(
        Dictionary<DateOnly, (int OnLeave, int Available)> dailyAvailability,
        int totalTeamLeaders,
        TeamLeaderAnalyticsRulesDto rules,
        List<ApplicationUser> teamLeaders)
    {
        var criticalPeriods = new List<TeamLeaderCriticalPeriodDto>();
        var currentPeriodStart = (DateOnly?)null;
        var currentPeriodEnd = (DateOnly?)null;
        var maxConcurrentInPeriod = 0;
        
        foreach (var day in dailyAvailability.OrderBy(d => d.Key))
        {
            var isCritical = day.Value.Available < rules.MinimumTeamLeadersRequired ||
                            day.Value.OnLeave > rules.MaxConcurrentLeavePerArea;
            
            if (isCritical)
            {
                currentPeriodStart ??= day.Key;
                currentPeriodEnd = day.Key;
                maxConcurrentInPeriod = Math.Max(maxConcurrentInPeriod, day.Value.OnLeave);
            }
            else if (currentPeriodStart.HasValue)
            {
                // End of critical period
                var severity = DetermineSeverity(maxConcurrentInPeriod, totalTeamLeaders, rules);
                var operationalRiskScore = CalculateOperationalRiskScore(
                    maxConcurrentInPeriod, 
                    totalTeamLeaders, 
                    currentPeriodStart.HasValue && currentPeriodEnd.HasValue ? currentPeriodEnd.Value.DayNumber - currentPeriodStart.Value.DayNumber + 1 : 0,
                    dailyAvailability.Count,
                    rules);
                
                criticalPeriods.Add(new TeamLeaderCriticalPeriodDto
                {
                    StartDate = currentPeriodStart.Value,
                    EndDate = currentPeriodEnd.Value,
                    Severity = severity,
                    RootCause = DetermineRootCause(maxConcurrentInPeriod, totalTeamLeaders, rules),
                    AffectedHangarShop = GetAffectedHangarShop(teamLeaders),
                    AffectedShifts = "All Shifts", // Simplified - could be enhanced with shift data
                    OperationalImpact = DetermineOperationalImpact(severity, maxConcurrentInPeriod, totalTeamLeaders),
                    RecommendedAction = DetermineRecommendedAction(severity, maxConcurrentInPeriod, totalTeamLeaders, rules),
                    TeamLeadersOnLeave = maxConcurrentInPeriod,
                    RemainingSupervisors = totalTeamLeaders - maxConcurrentInPeriod,
                    OperationalRiskScore = operationalRiskScore
                });
                
                currentPeriodStart = null;
                currentPeriodEnd = null;
                maxConcurrentInPeriod = 0;
            }
        }
        
        // Handle ongoing critical period at end of year
        if (currentPeriodStart.HasValue)
        {
            var severity = DetermineSeverity(maxConcurrentInPeriod, totalTeamLeaders, rules);
            var operationalRiskScore = CalculateOperationalRiskScore(
                maxConcurrentInPeriod, 
                totalTeamLeaders, 
                currentPeriodStart.HasValue && currentPeriodEnd.HasValue ? currentPeriodEnd.Value.DayNumber - currentPeriodStart.Value.DayNumber + 1 : 0,
                dailyAvailability.Count,
                rules);
            
            criticalPeriods.Add(new TeamLeaderCriticalPeriodDto
            {
                StartDate = currentPeriodStart.Value,
                EndDate = currentPeriodEnd.Value,
                Severity = severity,
                RootCause = DetermineRootCause(maxConcurrentInPeriod, totalTeamLeaders, rules),
                AffectedHangarShop = GetAffectedHangarShop(teamLeaders),
                AffectedShifts = "All Shifts",
                OperationalImpact = DetermineOperationalImpact(severity, maxConcurrentInPeriod, totalTeamLeaders),
                RecommendedAction = DetermineRecommendedAction(severity, maxConcurrentInPeriod, totalTeamLeaders, rules),
                TeamLeadersOnLeave = maxConcurrentInPeriod,
                RemainingSupervisors = totalTeamLeaders - maxConcurrentInPeriod,
                OperationalRiskScore = operationalRiskScore
            });
        }
        
        return criticalPeriods;
    }
    
    private string DetermineSeverity(int concurrentLeave, int totalTeamLeaders, TeamLeaderAnalyticsRulesDto rules)
    {
        var ratio = totalTeamLeaders > 0 ? (decimal)concurrentLeave / totalTeamLeaders : 0;
        
        if (ratio >= 1 - rules.CriticalCoverageThreshold) return "Critical";
        if (ratio >= 1 - rules.HighRiskThreshold) return "High";
        if (ratio >= 1 - rules.ModerateRiskThreshold) return "Medium";
        return "Low";
    }
    
    private string DetermineRootCause(int concurrentLeave, int totalTeamLeaders, TeamLeaderAnalyticsRulesDto rules)
    {
        if (concurrentLeave > rules.MaxConcurrentLeavePerArea)
            return $"Exceeds maximum concurrent leave threshold ({rules.MaxConcurrentLeavePerArea})";
        if (totalTeamLeaders - concurrentLeave < rules.MinimumTeamLeadersRequired)
            return $"Falls below minimum supervisors required ({rules.MinimumTeamLeadersRequired})";
        return "High leadership concentration during operational period";
    }
    
    private string GetAffectedHangarShop(List<ApplicationUser> teamLeaders)
    {
        var hangarNames = teamLeaders.Where(tl => tl.HangarId.HasValue).Select(tl => tl.Hangar?.Name).Distinct().ToList();
        var shopNames = teamLeaders.Where(tl => tl.ShopId.HasValue).Select(tl => tl.Shop?.Name).Distinct().ToList();
        
        var areas = new List<string>();
        if (hangarNames.Any()) areas.AddRange(hangarNames);
        if (shopNames.Any()) areas.AddRange(shopNames);
        
        return areas.Any() ? string.Join(", ", areas) : "Multiple Areas";
    }
    
    private string DetermineOperationalImpact(string severity, int concurrentLeave, int totalTeamLeaders)
    {
        return severity switch
        {
            "Critical" => "Maintenance supervision below operational minimum - high safety risk",
            "High" => "Supervisory capacity significantly reduced - operational delays likely",
            "Medium" => "Moderate supervision reduction - monitor closely",
            "Low" => "Minor impact on operations",
            _ => "Unknown impact"
        };
    }
    
    private string DetermineRecommendedAction(string severity, int concurrentLeave, int totalTeamLeaders, TeamLeaderAnalyticsRulesDto rules)
    {
        return severity switch
        {
            "Critical" => "Reject overlapping leave requests or assign backup Team Leaders immediately",
            "High" => "Review leave schedule and consider redistributing leave dates",
            "Medium" => "Monitor situation and prepare contingency plans",
            "Low" => "Continue normal operations with awareness",
            _ => "No action required"
        };
    }
    
    private List<string> GenerateInsights(
        TeamLeaderSummaryMetricsDto summaryMetrics,
        List<TeamLeaderMonthlyTrendDto> monthlyTrends,
        List<TeamLeaderCriticalPeriodDto> criticalPeriods,
        List<ApplicationUser> teamLeaders)
    {
        var insights = new List<string>();
        
        // Leadership coverage insight
        if (summaryMetrics.LeadershipCoverageRatio < 0.7m)
        {
            insights.Add($"Leadership coverage ratio ({summaryMetrics.LeadershipCoverageRatio:P0}) below optimal threshold - consider redistributing leave.");
        }
        
        // Peak concurrent leave insight
        if (summaryMetrics.PeakConcurrentTeamLeadersOnLeave > 2)
        {
            insights.Add($"Peak concurrent Team Leader leave ({summaryMetrics.PeakConcurrentTeamLeadersOnLeave}) exceeds recommended maximum - review {summaryMetrics.PeakLeaveDay}.");
        }
        
        // Critical periods insight
        if (criticalPeriods.Any(cp => cp.Severity == "Critical"))
        {
            var criticalCount = criticalPeriods.Count(cp => cp.Severity == "Critical");
            insights.Add($"{criticalCount} critical period(s) detected requiring immediate management attention.");
        }
        
        // Monthly trend insight
        var highPressureMonths = monthlyTrends.Where(m => m.LeadershipPressureIndex > 0.7m).ToList();
        if (highPressureMonths.Any())
        {
            var monthNames = string.Join(", ", highPressureMonths.Select(m => m.Month));
            insights.Add($"High leadership pressure detected in: {monthNames} - plan accordingly.");
        }
        
        // Supervision stability insight
        if (summaryMetrics.SupervisionStabilityIndex < 0.8m)
        {
            insights.Add($"Supervision stability index ({summaryMetrics.SupervisionStabilityIndex:P0}) indicates fluctuating leadership availability.");
        }
        
        // Risk trend insight
        var increasingRiskMonths = monthlyTrends.Where(m => m.RiskTrend == "Increasing").ToList();
        if (increasingRiskMonths.Count >= 3)
        {
            insights.Add("Leadership pressure trend increasing over multiple months - proactive planning recommended.");
        }
        
        // Operational readiness insight
        if (summaryMetrics.MaintenanceReadinessScore < 0.7m)
        {
            insights.Add($"Maintenance readiness score ({summaryMetrics.MaintenanceReadinessScore:P0}) below target - assess operational risk.");
        }
        
        if (!insights.Any())
        {
            insights.Add("Operational supervision remains stable with acceptable leadership coverage throughout the year.");
        }
        
        return insights;
    }
    
    private async Task<string> GetScopeNameAsync(Guid? sectionId, Guid? hangarId, Guid? shopId, CancellationToken cancellationToken)
    {
        if (shopId.HasValue)
        {
            var shop = await dbContext.Shops
                .AsNoTracking()
                .Where(s => s.Id == shopId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
            return $"Shop: {shop ?? "Unknown"}";
        }
        if (hangarId.HasValue)
        {
            var hangar = await dbContext.Hangars
                .AsNoTracking()
                .Where(h => h.Id == hangarId.Value)
                .Select(h => h.Name)
                .FirstOrDefaultAsync(cancellationToken);
            return $"Hangar: {hangar ?? "Unknown"}";
        }
        if (sectionId.HasValue)
        {
            var section = await dbContext.Sections
                .AsNoTracking()
                .Where(s => s.Id == sectionId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
            return $"Section: {section ?? "Unknown"}";
        }
        return "Enterprise";
    }
}
