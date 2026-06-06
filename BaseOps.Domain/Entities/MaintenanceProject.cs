using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public enum MaintenanceProjectStatus
{
    Draft,
    InProgress,
    OnHold,
    Completed,
    Cancelled
}

public enum ProjectType
{
    Dehangaring,
    Maintenance,
    Modification,
    Inspection
}

public sealed class MaintenanceProject : AuditableEntity
{
    public string ProjectNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AircraftRegistration { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public string FleetType { get; set; } = string.Empty;
    public ProjectType Type { get; set; }
    public MaintenanceProjectStatus Status { get; set; } = MaintenanceProjectStatus.Draft;
    public DateTime ScheduledStartDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime ScheduledEndDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public int CompletionPercentage { get; set; }
    public Guid SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public Guid? ProjectManagerUserId { get; set; }
    public string? Remarks { get; set; }
    public bool IsDelayed { get; set; }
    public string? DelayReason { get; set; }

    // Navigation properties
    public Section? Section { get; set; }
    public Hangar? Hangar { get; set; }
    public Shop? Shop { get; set; }
    public ApplicationUser? ProjectManagerUser { get; set; }
    public ICollection<DailyProgressLog> ProgressLogs { get; set; } = new List<DailyProgressLog>();
    public ICollection<PartFollowUp> PartFollowUps { get; set; } = new List<PartFollowUp>();
}

public sealed class DailyProgressLog : AuditableEntity
{
    public Guid MaintenanceProjectId { get; set; }
    public DateTime LogDate { get; set; }
    public string WorkPerformed { get; set; } = string.Empty;
    public int PlannedHours { get; set; }
    public int ActualHours { get; set; }
    public int ManpowerCount { get; set; }
    public string? IssuesEncountered { get; set; }
    public string? NextDayPlan { get; set; }
    public bool IsSubmitted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? SubmittedByUserId { get; set; }

    // Navigation properties
    public MaintenanceProject Project { get; set; } = null!;
    public ApplicationUser? SubmittedByUser { get; set; }
}

public sealed class PartFollowUp : AuditableEntity
{
    public Guid MaintenanceProjectId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RequiredBy { get; set; }
    public DateTime? OrderedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? Supplier { get; set; }
    public decimal? Cost { get; set; }
    public string? Remarks { get; set; }
    public Guid? AssignedToUserId { get; set; }

    // Navigation properties
    public MaintenanceProject Project { get; set; } = null!;
    public ApplicationUser? AssignedToUser { get; set; }
}
