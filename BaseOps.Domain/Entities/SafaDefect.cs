using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public enum DefectStatus
{
    Active,
    Cleared,
    WaitingForPart
}

public sealed class SafaDefect : AuditableEntity
{
    public Guid InspectionId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? SubCategory { get; set; }
    public string StandardDescription { get; set; } = string.Empty;
    public string ObservationFinding { get; set; } = string.Empty;
    public bool NeedToFix { get; set; }
    public DefectStatus Status { get; set; } = DefectStatus.Active;
    public string? CorrectiveAction { get; set; }
    public string? TaskCardCode { get; set; }
    public string? PartRequestId { get; set; }
    public string? Remarks { get; set; }
    public Guid? ActionTakenByUserId { get; set; }
    public DateTime? ActionTakenAt { get; set; }
    
    // Navigation properties
    public SafaInspection Inspection { get; set; } = null!;
    public ApplicationUser? ActionTakenByUser { get; set; }
}
