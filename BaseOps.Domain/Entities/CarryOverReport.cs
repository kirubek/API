using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public enum CarryOverReportStatus
{
    Draft,
    Submitted,
    Reviewed,
    Finalized
}

public enum CarryOverTaskType
{
    Routine,
    NonRoutine,
    AD,
    CMR,
    UC,
    GP,
    EO,
    ER
}

public enum CarryOverDeferralReason
{
    Parts,
    Tools,
    GroundTime,
    Manpower,
    Weather,
    TechnicalDocumentation,
    Other
}

public enum CarryOverTaskOrigin
{
    Scheduled,
    NewRemark
}

public enum CarryOverTaskStatus
{
    Open,
    Deferred,
    Completed
}

public sealed class CarryOverReport : AuditableEntity
{
    public string ReportNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AircraftRegistration { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public CarryOverReportStatus Status { get; set; } = CarryOverReportStatus.Draft;
    public DateTime DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Guid SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public int CarryOverTasks { get; set; }
    public int CarryOverPercentage { get; set; }
    public string? Remarks { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComments { get; set; }
    public Guid? FinalizedByUserId { get; set; }
    public DateTime? FinalizedAt { get; set; }

    // Navigation properties
    public Section? Section { get; set; }
    public Hangar? Hangar { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
    public ApplicationUser? SubmittedByUser { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }
    public ApplicationUser? FinalizedByUser { get; set; }
    public ICollection<CarryOverTask> Tasks { get; set; } = new List<CarryOverTask>();
    public ICollection<CarryOverReview> Reviews { get; set; } = new List<CarryOverReview>();
}

public sealed class CarryOverTask : AuditableEntity
{
    public Guid CarryOverReportId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public CarryOverTaskStatus Status { get; set; } = CarryOverTaskStatus.Open;
    public CarryOverTaskType TaskType { get; set; }
    public CarryOverDeferralReason DeferralReason { get; set; }
    public string? DeferralDetails { get; set; }
    public CarryOverTaskOrigin DeferredTaskOrigin { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Notes { get; set; }
    public string? TaskCardNumber { get; set; }
    public string? PartRequestId { get; set; }

    // Navigation properties
    public CarryOverReport Report { get; set; } = null!;
    public ApplicationUser? AssignedToUser { get; set; }
}

public sealed class CarryOverReview : AuditableEntity
{
    public Guid CarryOverReportId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public bool Approved { get; set; }
    public string? Comments { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime ReviewedAt { get; set; }

    // Navigation properties
    public CarryOverReport Report { get; set; } = null!;
    public ApplicationUser ReviewerUser { get; set; } = null!;
}
