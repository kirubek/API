using BaseOps.Application.EmployeeProfiles;
using BaseOps.Application.EmployeeProfiles.DTOs;
using BaseOps.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
[Route("api/employee-profile")]
public sealed class EmployeeProfileController(IEmployeeProfileService profileService) : ControllerBase
{
    [HttpGet("me")]
    [HttpGet("user/profile")]
    public async Task<ActionResult<EmployeeProfileResponseDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var profile = await profileService.GetMyProfileAsync(userId.Value, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("me")]
    [HttpPut("user/profile")]
    public async Task<ActionResult<EmployeeProfileResponseDto>> UpdateMyProfile(
        [FromBody] UpdateEmployeeProfileDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var updatedProfile = await profileService.UpdateMyProfileAsync(userId.Value, dto, cancellationToken);
        return Ok(updatedProfile);
    }

    [HttpGet]
    [HttpGet("section")]
    [HttpGet("hangar")]
    [HttpGet("directory")]
    public async Task<ActionResult<PaginatedResult<EmployeeProfileResponseDto>>> GetEmployeeProfiles(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? sectionId = null,
        [FromQuery] Guid? hangarId = null,
        [FromQuery] Guid? shopId = null,
        [FromQuery] Guid? teamLeaderId = null,
        [FromQuery] string? employeeId = null,
        [FromQuery] string? firstName = null,
        [FromQuery] string? lastName = null,
        [FromQuery] string? position = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var result = await profileService.GetEmployeeProfilesAsync(
            userId.Value,
            pageNumber,
            pageSize,
            sectionId,
            hangarId,
            shopId,
            teamLeaderId,
            employeeId,
            firstName,
            lastName,
            position,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeProfileResponseDto>> GetProfileById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var profile = await profileService.GetProfileByIdAsync(userId.Value, id, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SystemAdmin,Director")]
    public async Task<ActionResult<EmployeeProfileResponseDto>> AdminUpdateProfile(
        Guid id,
        [FromBody] AdminUpdateEmployeeProfileDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var updatedProfile = await profileService.AdminUpdateProfileAsync(userId.Value, id, dto, cancellationToken);
        return Ok(updatedProfile);
    }
}
