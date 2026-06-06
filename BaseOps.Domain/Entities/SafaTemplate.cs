using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class SafaTemplate : AuditableEntity
{
    public SafaInspectionType InspectionType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TemplateJson { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}
