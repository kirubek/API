namespace BaseOps.Application.DailyStatusReport.DTOs;

using BaseOps.Domain.Entities;

public record DailyStatusReportDto
{
    public Guid Id { get; init; }
    public string ReportNumber { get; init; } = string.Empty;
    public string AircraftRegistration { get; init; } = string.Empty;
    public string AircraftType { get; init; } = string.Empty;
    public string? Fleet { get; init; }
    public string? MaintenanceVisit { get; init; }
    public string? CheckType { get; init; }
    public DateTime ReportDate { get; init; }
    public DailyStatusReportStatus Status { get; init; }
    public Guid SectionId { get; init; }
    public Guid? HangarId { get; init; }
    public string? SectionName { get; init; }
    public string? HangarName { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastUpdatedAt { get; init; }
    public string? SubmittedByName { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public string? ReviewedByName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ApprovedByName { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? RejectionReason { get; init; }
    public TaskStatusDto[] TaskStatuses { get; init; } = Array.Empty<TaskStatusDto>();
    public PartIssueDto[] PartIssues { get; init; } = Array.Empty<PartIssueDto>();
    public MajorFindingDto[] MajorFindings { get; init; } = Array.Empty<MajorFindingDto>();
    public ImportHistoryDto[] ImportHistories { get; init; } = Array.Empty<ImportHistoryDto>();
}

public record CreateDailyStatusReportDto
{
    public string AircraftRegistration { get; init; } = string.Empty;
    public string AircraftType { get; init; } = string.Empty;
    public string? Fleet { get; init; }
    public string? MaintenanceVisit { get; init; }
    public string? CheckType { get; init; }
    public DateTime ReportDate { get; init; }
    public Guid SectionId { get; init; }
    public Guid? HangarId { get; init; }
}

public record UpdateDailyStatusReportDto
{
    public string? AircraftRegistration { get; init; }
    public string? AircraftType { get; init; }
    public string? Fleet { get; init; }
    public string? MaintenanceVisit { get; init; }
    public string? CheckType { get; init; }
    public DateTime? ReportDate { get; init; }
    public Guid? HangarId { get; init; }
}

public record TaskStatusDto
{
    public Guid Id { get; init; }
    public string TaskName { get; init; } = string.Empty;
    public string TaskId { get; init; } = string.Empty;
    public string TaskType { get; init; } = string.Empty;
    public PhaseEnum Phase { get; init; }
    public TaskStatusEnum Status { get; init; }
    public int SerialNumber { get; init; }
}

public record CreateTaskStatusDto
{
    public string TaskName { get; init; } = string.Empty;
    public string TaskId { get; init; } = string.Empty;
    public string TaskType { get; init; } = string.Empty;
    public PhaseEnum Phase { get; init; }
    public TaskStatusEnum Status { get; init; }
    public int SerialNumber { get; init; }
}

public record PartIssueDto
{
    public Guid Id { get; init; }
    public PartIssueType IssueType { get; init; }
    public int ItemNumber { get; init; }
    public string? Task { get; init; }
    public string PartNumber { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? RID { get; init; }
    public int Quantity { get; init; }
    public DateTime? DateRequested { get; init; }
    public DateTime? DateReceived { get; init; }
    public DateTime? DateRobbed { get; init; }
    public DateTime? ClosedDate { get; init; }
    public string? PONumber { get; init; }
    public string? ResponsibleBuyer { get; init; }
    public string? Vendor { get; init; }
    public string? DonorAircraft { get; init; }
    public string? RecipientAircraft { get; init; }
    public string? Status { get; init; }
    public string? Remark { get; init; }
    public string? EDD { get; init; }
    public string? Resolution { get; init; }
    public string? ClosedBy { get; init; }
}

public record CreatePartIssueDto
{
    public PartIssueType IssueType { get; init; }
    public int ItemNumber { get; init; }
    public string? Task { get; init; }
    public string PartNumber { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? RID { get; init; }
    public int Quantity { get; init; }
    public DateTime? DateRequested { get; init; }
    public DateTime? DateReceived { get; init; }
    public DateTime? DateRobbed { get; init; }
    public DateTime? ClosedDate { get; init; }
    public string? PONumber { get; init; }
    public string? ResponsibleBuyer { get; init; }
    public string? Vendor { get; init; }
    public string? DonorAircraft { get; init; }
    public string? RecipientAircraft { get; init; }
    public string? Status { get; init; }
    public string? Remark { get; init; }
    public string? EDD { get; init; }
    public string? Resolution { get; init; }
    public string? ClosedBy { get; init; }
}

public record MajorFindingDto
{
    public Guid Id { get; init; }
    public string FindingNumber { get; init; } = string.Empty;
    public string? ATAChapter { get; init; }
    public string Description { get; init; } = string.Empty;
    public FindingSeverity Severity { get; init; }
    public FindingStatus Status { get; init; }
    public DateTime RaisedDate { get; init; }
    public string? Owner { get; init; }
    public DateTime? TargetClosureDate { get; init; }
    public DateTime? ClosureDate { get; init; }
    public string? Remarks { get; init; }
}

public record CreateMajorFindingDto
{
    public string FindingNumber { get; init; } = string.Empty;
    public string? ATAChapter { get; init; }
    public string Description { get; init; } = string.Empty;
    public FindingSeverity Severity { get; init; }
    public FindingStatus Status { get; init; }
    public DateTime RaisedDate { get; init; }
    public string? Owner { get; init; }
    public DateTime? TargetClosureDate { get; init; }
    public DateTime? ClosureDate { get; init; }
    public string? Remarks { get; init; }
}

public record ImportHistoryDto
{
    public Guid Id { get; init; }
    public ImportType ImportType { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string UploadedByName { get; init; } = string.Empty;
    public DateTime UploadDate { get; init; }
    public int RecordCount { get; init; }
    public ImportStatus ImportStatus { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ColumnMapping { get; init; }
}

public record ImportExcelDto
{
    public Guid ReportId { get; init; }
    public ImportType ImportType { get; init; }
    public string FileName { get; init; } = string.Empty;
    public byte[] FileContent { get; init; } = Array.Empty<byte>();
    public string? ColumnMapping { get; init; }
}

public record ImportValidationResultDto
{
    public bool IsValid { get; init; }
    public string[] Errors { get; init; } = Array.Empty<string>();
    public string[] Warnings { get; init; } = Array.Empty<string>();
    public int SuccessfulRecords { get; init; }
    public int FailedRecords { get; init; }
}

public record PhaseProgressDto
{
    public TaskStatusEnum Status { get; init; }
    public int Incoming { get; init; }
    public int OpenUp { get; init; }
    public int Clean { get; init; }
    public int FunAndOpr { get; init; }
    public int CDP { get; init; }
    public int EO_AD { get; init; }
    public int Dis_Rep { get; init; }
    public int LubAndSer { get; init; }
    public int DBC { get; init; }
    public int Remarks { get; init; }
    public int CloseUp { get; init; }
    public int Final { get; init; }
    public int DeHan { get; init; }
    public int Total { get; init; }
}

public record OverallStatusDto
{
    public string TypeOfTask { get; init; } = string.Empty;
    public int Complete { get; init; }
    public int Unassign { get; init; }
    public int InWork { get; init; }
    public int Pause { get; init; }
    public int Active { get; init; }
    public int Total { get; init; }
}

public record DashboardSummaryDto
{
    public int TotalReports { get; init; }
    public int DraftReports { get; init; }
    public int SubmittedReports { get; init; }
    public int ApprovedReports { get; init; }
    public int PendingReview { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int InWorkTasks { get; init; }
    public int TotalPartIssues { get; init; }
    public int CriticalParts { get; init; }
    public int TotalFindings { get; init; }
    public int OpenFindings { get; init; }
    public int CriticalFindings { get; init; }
}

public record AnalyticsDto
{
    public Dictionary<string, int> TaskStatusDistribution { get; init; } = new();
    public Dictionary<string, int> PhaseDistribution { get; init; } = new();
    public double CompletionPercentage { get; init; }
    public Dictionary<string, int> PartIssueDistribution { get; init; } = new();
    public Dictionary<string, int> FindingSeverityDistribution { get; init; } = new();
    public Dictionary<string, int> FindingStatusDistribution { get; init; } = new();
}

public record ReportDecisionDto
{
    public string? RejectionReason { get; init; }
}

public record ExportRequestDto
{
    public Guid ReportId { get; init; }
    public string Format { get; init; } = "PDF";
}
