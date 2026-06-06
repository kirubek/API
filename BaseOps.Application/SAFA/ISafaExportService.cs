using BaseOps.Application.SAFA.DTOs;

namespace BaseOps.Application.SAFA;

public interface ISafaExportService
{
    Task<byte[]> ExportInspectionsToPdfAsync(List<SafaInspectionListDto> inspections, CancellationToken cancellationToken = default);
    Task<byte[]> ExportInspectionsToExcelAsync(List<SafaInspectionListDto> inspections, CancellationToken cancellationToken = default);
    Task<byte[]> ExportInspectionsToCsvAsync(List<SafaInspectionListDto> inspections, CancellationToken cancellationToken = default);
    Task<byte[]> ExportDefectsToPdfAsync(List<SafaDefectDto> defects, CancellationToken cancellationToken = default);
    Task<byte[]> ExportDefectsToExcelAsync(List<SafaDefectDto> defects, CancellationToken cancellationToken = default);
    Task<byte[]> ExportDefectsToCsvAsync(List<SafaDefectDto> defects, CancellationToken cancellationToken = default);
}
