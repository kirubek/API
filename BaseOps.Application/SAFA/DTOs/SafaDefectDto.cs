namespace BaseOps.Application.SAFA.DTOs;

using BaseOps.Domain.Entities;

public record CreateSafaDefectDto
{
    public Guid InspectionId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string? SubCategory { get; init; }
    public string StandardDescription { get; init; } = string.Empty;
    public string ObservationFinding { get; init; } = string.Empty;
    public bool NeedToFix { get; init; }
}

public record UpdateSafaDefectDto
{
    public string? ObservationFinding { get; init; }
    public bool? NeedToFix { get; init; }
}

public record TakeCorrectiveActionDto
{
    public string CorrectiveAction { get; init; } = string.Empty;
    public string? TaskCardCode { get; init; }
    public string? PartRequestId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Remarks { get; init; }
}

public record UpdateDefectStatusDto
{
    public string Status { get; init; } = string.Empty;
    public string? PartRequestId { get; init; }
}

public record SafaDefectDto
{
    public Guid Id { get; init; }
    public Guid InspectionId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string? SubCategory { get; init; }
    public string StandardDescription { get; init; } = string.Empty;
    public string ObservationFinding { get; init; } = string.Empty;
    public bool NeedToFix { get; init; }
    public DefectStatus Status { get; init; }
    public string? CorrectiveAction { get; init; }
    public string? TaskCardCode { get; init; }
    public string? PartRequestId { get; init; }
    public string? Remarks { get; init; }
    public Guid? ActionTakenByUserId { get; init; }
    public string? ActionTakenByName { get; init; }
    public DateTime? ActionTakenAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
