using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class Workspace : AuditableEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
