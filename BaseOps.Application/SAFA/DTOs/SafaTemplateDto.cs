namespace BaseOps.Application.SAFA.DTOs;

using BaseOps.Domain.Entities;

public record SafaTemplateDto
{
    public Guid Id { get; init; }
    public SafaInspectionType InspectionType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TemplateJson { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateSafaTemplateDto
{
    public SafaInspectionType InspectionType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TemplateJson { get; init; } = string.Empty;
}

public record UpdateSafaTemplateDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? TemplateJson { get; init; }
    public bool? IsActive { get; init; }
}
