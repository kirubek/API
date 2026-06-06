using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class Hangar : AuditableEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public Guid SectionId { get; set; }
    public Section? Section { get; set; }
}
