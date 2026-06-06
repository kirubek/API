using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class UserWorkspaceAssignment : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? HangarId { get; set; }
    public Guid? ShopId { get; set; }
    public required string WorkspaceType { get; set; }
    public required string AssignmentType { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public bool IsActive { get; set; } = true;
    public ApplicationUser? User { get; set; }
    public Section? Section { get; set; }
    public Hangar? Hangar { get; set; }
    public Shop? Shop { get; set; }
}
