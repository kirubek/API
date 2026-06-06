namespace BaseOps.Application.Common;

public sealed record UserScope(
    Guid UserId,
    string Role,
    Guid? SectionId,
    Guid? HangarId,
    Guid? ShopId,
    bool HasOperationalScope,
    bool HasProductionPlannerAccess,
    bool CanCreateAumsReports,
    bool CanCreateCarryOverReports,
    bool CanCreatePostMortemReports);
