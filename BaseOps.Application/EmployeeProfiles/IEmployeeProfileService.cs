using BaseOps.Application.EmployeeProfiles.DTOs;
using BaseOps.Application.Common;

namespace BaseOps.Application.EmployeeProfiles;

public interface IEmployeeProfileService
{
    Task<EmployeeProfileResponseDto> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<EmployeeProfileResponseDto> UpdateMyProfileAsync(Guid userId, UpdateEmployeeProfileDto dto, CancellationToken cancellationToken = default);
    Task<EmployeeProfileResponseDto> GetProfileByIdAsync(Guid currentUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<PaginatedResult<EmployeeProfileResponseDto>> GetEmployeeProfilesAsync(
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        Guid? sectionId = null,
        Guid? hangarId = null,
        Guid? shopId = null,
        Guid? teamLeaderId = null,
        string? employeeId = null,
        string? firstName = null,
        string? lastName = null,
        string? position = null,
        CancellationToken cancellationToken = default);
    Task<EmployeeProfileResponseDto> AdminUpdateProfileAsync(Guid currentUserId, Guid targetUserId, AdminUpdateEmployeeProfileDto dto, CancellationToken cancellationToken = default);
}
