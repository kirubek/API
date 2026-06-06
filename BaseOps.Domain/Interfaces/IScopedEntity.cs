namespace BaseOps.Domain.Interfaces;

public interface IScopedEntity
{
    Guid SectionId { get; }
    Guid? HangarId { get; }
    Guid? ShopId { get; }
    Guid? AssignedUserId { get; }
}
