using BaseOps.Application.SAFA.DTOs;
using BaseOps.Domain.Entities;

namespace BaseOps.Application.SAFA;

public interface ISafaService
{
    // Inspection operations
    Task<SafaInspectionDto> CreateInspectionAsync(CreateSafaInspectionDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<SafaInspectionDto?> GetInspectionAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<SafaInspectionDto> UpdateInspectionAsync(Guid id, UpdateSafaInspectionDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteInspectionAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<SafaInspectionDto> SubmitInspectionAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<(List<SafaInspectionListDto> Items, int TotalCount, int TotalPages)> GetInspectionsAsync(
        int page, int pageSize, Guid userId,
        SafaInspectionType? inspectionType = null,
        InspectionStatus? status = null,
        Guid? sectionId = null,
        Guid? hangarId = null,
        Guid? shopId = null,
        string? aircraftRegistration = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    // Defect operations
    Task<SafaDefectDto> CreateDefectAsync(CreateSafaDefectDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<SafaDefectDto?> GetDefectAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<SafaDefectDto> UpdateDefectAsync(Guid id, UpdateSafaDefectDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteDefectAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<SafaDefectDto> TakeCorrectiveActionAsync(Guid id, TakeCorrectiveActionDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<SafaDefectDto> UpdateDefectStatusAsync(Guid id, UpdateDefectStatusDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<List<SafaDefectDto>> GetDefectsByInspectionAsync(Guid inspectionId, Guid userId, CancellationToken cancellationToken = default);

    // Template operations
    Task<SafaTemplateDto> CreateTemplateAsync(CreateSafaTemplateDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<SafaTemplateDto?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SafaTemplateDto> UpdateTemplateAsync(Guid id, UpdateSafaTemplateDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<SafaTemplateDto?> GetActiveTemplateAsync(SafaInspectionType inspectionType, CancellationToken cancellationToken = default);
    Task<List<SafaTemplateDto>> GetTemplatesAsync(SafaInspectionType? inspectionType = null, CancellationToken cancellationToken = default);

    // Analytics operations
    Task<SafaDashboardDto> GetDashboardAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<SafaAnalyticsDto> GetAnalyticsAsync(Guid userId, SafaInspectionType? inspectionType = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<DetailedAnalyticsDto> GetDetailedAnalyticsAsync(Guid userId, SafaInspectionType inspectionType, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}

// Workflow validation helpers
public static class InspectionStatusTransitions
{
    private static readonly Dictionary<InspectionStatus, InspectionStatus[]> ValidTransitions = new()
    {
        [InspectionStatus.Draft] = new[] { InspectionStatus.InProgress, InspectionStatus.Submitted },
        [InspectionStatus.InProgress] = new[] { InspectionStatus.Submitted },
        [InspectionStatus.Submitted] = new[] { InspectionStatus.Completed },
        [InspectionStatus.Completed] = Array.Empty<InspectionStatus>() // Terminal state
    };

    public static bool IsValidTransition(InspectionStatus from, InspectionStatus to)
    {
        return ValidTransitions[from].Contains(to);
    }
}

public static class DefectStatusTransitions
{
    private static readonly Dictionary<DefectStatus, DefectStatus[]> ValidTransitions = new()
    {
        [DefectStatus.Active] = new[] { DefectStatus.Cleared, DefectStatus.WaitingForPart },
        [DefectStatus.WaitingForPart] = new[] { DefectStatus.Cleared },
        [DefectStatus.Cleared] = Array.Empty<DefectStatus>() // Terminal state
    };

    public static bool IsValidTransition(DefectStatus from, DefectStatus to)
    {
        return ValidTransitions[from].Contains(to);
    }
}
