using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public enum DailyStatusReportStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Exported
}

public enum TaskStatusEnum
{
    Complete,
    InWork,
    Pause,
    Unassign,
    Active
}

public enum PhaseEnum
{
    Incoming,
    OpenUp,
    Clean,
    FunAndOpr,
    CDP,
    EO_AD,
    Dis_Rep,
    LubAndSer,
    DBC,
    Remarks,
    CloseUp,
    Final,
    DeHangaring
}

public enum PartIssueType
{
    Critical,
    Robbed,
    Received,
    CaseClosed
}

public enum FindingSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum FindingStatus
{
    Open,
    InProgress,
    Closed,
    Deferred
}

public enum ImportType
{
    TaskStatus,
    PartIssues
}

public enum ImportStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    RolledBack
}

public sealed class DailyStatusReport : AuditableEntity
{
    public string ReportNumber { get; set; } = string.Empty;
    public string AircraftRegistration { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public string? Fleet { get; set; }
    public string? MaintenanceVisit { get; set; }
    public string? CheckType { get; set; }
    public DateTime ReportDate { get; set; }
    public DailyStatusReportStatus Status { get; set; } = DailyStatusReportStatus.Draft;
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
    public ICollection<TaskStatus> TaskStatuses { get; set; } = new List<TaskStatus>();
    public ICollection<PartIssue> PartIssues { get; set; } = new List<PartIssue>();
    public ICollection<MajorFinding> MajorFindings { get; set; } = new List<MajorFinding>();
    public ICollection<ImportHistory> ImportHistories { get; set; } = new List<ImportHistory>();
}

public sealed class TaskStatus : AuditableEntity
{
    public Guid DailyStatusReportId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public PhaseEnum Phase { get; set; }
    public TaskStatusEnum Status { get; set; }
    public int SerialNumber { get; set; }

    // Navigation properties
    public DailyStatusReport Report { get; set; } = null!;
}

public sealed class PartIssue : AuditableEntity
{
    public Guid DailyStatusReportId { get; set; }
    public PartIssueType IssueType { get; set; }
    public int ItemNumber { get; set; }
    public string? Task { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RID { get; set; }
    public int Quantity { get; set; }
    public DateTime? DateRequested { get; set; }
    public DateTime? DateReceived { get; set; }
    public DateTime? DateRobbed { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string? PONumber { get; set; }
    public string? ResponsibleBuyer { get; set; }
    public string? Vendor { get; set; }
    public string? DonorAircraft { get; set; }
    public string? RecipientAircraft { get; set; }
    public string? Status { get; set; }
    public string? Remark { get; set; }
    public string? EDD { get; set; }
    public string? Resolution { get; set; }
    public string? ClosedBy { get; set; }

    // Navigation properties
    public DailyStatusReport Report { get; set; } = null!;
}

public sealed class MajorFinding : AuditableEntity
{
    public Guid DailyStatusReportId { get; set; }
    public string FindingNumber { get; set; } = string.Empty;
    public string? ATAChapter { get; set; }
    public string Description { get; set; } = string.Empty;
    public FindingSeverity Severity { get; set; }
    public FindingStatus Status { get; set; }
    public DateTime RaisedDate { get; set; }
    public string? Owner { get; set; }
    public DateTime? TargetClosureDate { get; set; }
    public DateTime? ClosureDate { get; set; }
    public string? Remarks { get; set; }

    // Navigation properties
    public DailyStatusReport Report { get; set; } = null!;
}

public sealed class ImportHistory : AuditableEntity
{
    public Guid DailyStatusReportId { get; set; }
    public ImportType ImportType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadDate { get; set; }
    public int RecordCount { get; set; }
    public ImportStatus ImportStatus { get; set; } = ImportStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ColumnMapping { get; set; }

    // Navigation properties
    public DailyStatusReport Report { get; set; } = null!;
    public ApplicationUser UploadedByUser { get; set; } = null!;
}
