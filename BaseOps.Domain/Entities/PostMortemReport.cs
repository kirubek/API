using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public enum PostMortemReportStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Exported
}

public enum PostMortemTaskType
{
    Routine,
    NonRoutine,
    EO,
    NRC
}

public enum PostMortemCheckType
{
    Digital,
    Paper
}

public sealed class PostMortemReport : AuditableEntity
{
    public string ReportNumber { get; set; } = string.Empty;
    public string WorkPackageId { get; set; } = string.Empty;
    public string WorkPackageDescription { get; set; } = string.Empty;
    public string AircraftRegistration { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public string HangaringStatus { get; set; } = string.Empty;
    public string DeHangaringStatus { get; set; } = string.Empty;
    public string TatStatus { get; set; } = string.Empty;
    public PostMortemCheckType CheckType { get; set; }
    public DateTime ScheduledIn { get; set; }
    public DateTime ActualIn { get; set; }
    public DateTime ScheduledOut { get; set; }
    public DateTime ActualOut { get; set; }
    public string IncomingDeviationReason { get; set; } = string.Empty;
    public string DeviationReasonDeHangaring { get; set; } = string.Empty;
    public decimal ScheduleTATHours { get; set; }
    public decimal ActualTATHours { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public PostMortemReportStatus Status { get; set; } = PostMortemReportStatus.Draft;
    public Guid SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation properties
    public Section? Section { get; set; }
    public Hangar? Hangar { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? SubmittedByUser { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }
    public ApplicationUser? ApprovedByUser { get; set; }
    public ICollection<PostMortemSlaRecord> SlaRecords { get; set; } = new List<PostMortemSlaRecord>();
    public ICollection<PostMortemCrsCompletion> CrsCompletions { get; set; } = new List<PostMortemCrsCompletion>();
    public ICollection<PostMortemTatRecord> TatRecords { get; set; } = new List<PostMortemTatRecord>();
    public ICollection<PostMortemPlanStability> PlanStabilityRecords { get; set; } = new List<PostMortemPlanStability>();
    public ICollection<PostMortemCarryOverTask> CarryOverTasks { get; set; } = new List<PostMortemCarryOverTask>();
}

public sealed class PostMortemSlaRecord : AuditableEntity
{
    public Guid PostMortemReportId { get; set; }
    public string SlaType { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Actual { get; set; } = string.Empty;
    public decimal Variance { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }

    // Navigation properties
    public PostMortemReport Report { get; set; } = null!;
}

public sealed class PostMortemCrsCompletion : AuditableEntity
{
    public Guid PostMortemReportId { get; set; }
    public string CrsNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedDate { get; set; }
    public string? Remarks { get; set; }

    // Navigation properties
    public PostMortemReport Report { get; set; } = null!;
}

public sealed class PostMortemTatRecord : AuditableEntity
{
    public Guid PostMortemReportId { get; set; }
    public string TaskDescription { get; set; } = string.Empty;
    public decimal PlannedHours { get; set; }
    public decimal ActualHours { get; set; }
    public decimal Variance { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DelayReason { get; set; } = string.Empty;
    public int HangaringNumber { get; set; }
    public string HangaringAC { get; set; } = string.Empty;
    public int DehangaringNumber { get; set; }
    public string DehangaringAC { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;

    // Navigation properties
    public PostMortemReport Report { get; set; } = null!;
}

public sealed class PostMortemPlanStability : AuditableEntity
{
    public Guid PostMortemReportId { get; set; }
    public string PlanVersion { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public int ChangeCount { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
    public string? Remarks { get; set; }

    // Navigation properties
    public PostMortemReport Report { get; set; } = null!;
}

public enum PostMortemCarryOverTaskStatus
{
    Open,
    Deferred,
    Completed
}

public sealed class PostMortemCarryOverTask : AuditableEntity
{
    public Guid PostMortemReportId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AircraftRegistration { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public PostMortemTaskType TaskType { get; set; }
    public string DeferralReason { get; set; } = string.Empty;
    public PostMortemCarryOverTaskStatus Status { get; set; } = PostMortemCarryOverTaskStatus.Open;
    public DateTime? TargetDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Remarks { get; set; }

    // Navigation properties
    public PostMortemReport Report { get; set; } = null!;
    public ApplicationUser? AssignedToUser { get; set; }
}
