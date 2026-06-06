namespace BaseOps.Application.SAFA.DTOs;

using BaseOps.Domain.Entities;

public record SafaDashboardDto
{
    public int TotalInspections { get; init; }
    public int Satisfactory { get; init; }
    public int Unsatisfactory { get; init; }
    public int OpenDefects { get; init; }
    public double ComplianceRate { get; init; }
    public List<InspectionByTypeDto> ByType { get; init; } = new();
    public List<RecentInspectionDto> RecentInspections { get; init; } = new();
}

public record InspectionByTypeDto
{
    public string Type { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record RecentInspectionDto
{
    public Guid Id { get; init; }
    public string InspectionType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string AircraftRegistration { get; init; } = string.Empty;
}

public record SafaAnalyticsDto
{
    public List<StatusCountDto> ByStatus { get; init; } = new();
    public List<InspectionByTypeDto> ByInspectionType { get; init; } = new();
    public List<SectionCountDto> BySection { get; init; } = new();
    public List<SeverityCountDto> FindingSeverity { get; init; } = new();
    public List<MonthlyTrendDto> MonthlyTrends { get; init; } = new();
}

public record StatusCountDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record SectionCountDto
{
    public string Section { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record SeverityCountDto
{
    public string Severity { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record MonthlyTrendDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int Count { get; init; }
    public int Completed { get; init; }
}

public record DetailedAnalyticsDto
{
    public int TotalDefects { get; init; }
    public int ActiveDefects { get; init; }
    public int ClearedDefects { get; init; }
    public int WaitingForPartDefects { get; init; }
    public double DefectClosureRate { get; init; }
    public double AvgResolutionTime { get; init; }
    public List<CategoryCountDto> DefectsByCategory { get; init; } = new();
    public List<StatusCountDto> DefectsByStatus { get; init; } = new();
    public List<FleetComparisonDto> FleetComparison { get; init; } = new();
    public List<HangarComparisonDto> HangarComparison { get; init; } = new();
    public List<MonthlyTrendDto> MonthlyTrend { get; init; } = new();
    public List<CategoryFleetHeatmapDto> CategoryFleetHeatmap { get; init; } = new();
}

public record CategoryCountDto
{
    public string Category { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record FleetComparisonDto
{
    public string Fleet { get; init; } = string.Empty;
    public int Defects { get; init; }
    public double ClosureRate { get; init; }
}

public record HangarComparisonDto
{
    public string Hangar { get; init; } = string.Empty;
    public int Active { get; init; }
    public int Cleared { get; init; }
    public int Waiting { get; init; }
    public int Total { get; init; }
}

public record CategoryFleetHeatmapDto
{
    public string Category { get; init; } = string.Empty;
    public int A350 { get; init; }
    public int B787 { get; init; }
    public int B777 { get; init; }
    public int B737 { get; init; }
}
