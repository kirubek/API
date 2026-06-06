using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class Section : AuditableEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public ICollection<Hangar> Hangars { get; set; } = new List<Hangar>();
    public ICollection<Shop> Shops { get; set; } = new List<Shop>();
}
