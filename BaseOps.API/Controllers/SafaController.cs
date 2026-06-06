using BaseOps.Application.SAFA;
using BaseOps.Application.SAFA.DTOs;
using BaseOps.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
public sealed class SafaController(ISafaService safaService, ILogger<SafaController> logger) : ControllerBase
{
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    // Inspection endpoints
    [HttpPost("api/safa/inspections")]
    [Authorize(Roles = "SafetyInspector,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> CreateInspection([FromBody] CreateSafaInspectionDto dto, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("CreateInspection endpoint called");
        logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
        logger.LogInformation("Request Content-Length: {ContentLength}", Request.ContentLength);
        
        // Log model state errors
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid");
            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    logger.LogWarning("Model state error for {Key}: {Error}", state.Key, error.ErrorMessage);
                }
            }
        }
        
        logger.LogInformation("CreateInspection called with dto: {Dto}", System.Text.Json.JsonSerializer.Serialize(dto));
        
        if (dto == null)
        {
            logger.LogWarning("CreateInspection called with null DTO - request body may be missing or malformed");
            return BadRequest(new { message = "Request body is required and cannot be null. Please ensure all required fields are provided." });
        }
        
        var userId = GetCurrentUserId();
        var result = await safaService.CreateInspectionAsync(dto, userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/safa/inspections")]
    public async Task<IActionResult> GetInspections(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SafaInspectionType? inspectionType = null,
        [FromQuery] InspectionStatus? status = null,
        [FromQuery] Guid? sectionId = null,
        [FromQuery] Guid? hangarId = null,
        [FromQuery] Guid? shopId = null,
        [FromQuery] string? aircraftRegistration = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var (items, totalCount, totalPages) = await safaService.GetInspectionsAsync(
            page, pageSize, userId, inspectionType, status, sectionId, hangarId, shopId,
            aircraftRegistration, startDate, endDate, cancellationToken);

        return Ok(new { items, totalCount, totalPages, currentPage = page, pageSize });
    }

    [HttpGet("api/safa/inspections/{id}")]
    public async Task<IActionResult> GetInspection(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await safaService.GetInspectionAsync(id, userId, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPut("api/safa/inspections/{id}")]
    [Authorize(Roles = "SafetyInspector,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> UpdateInspection(Guid id, [FromBody] UpdateSafaInspectionDto dto, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await safaService.UpdateInspectionAsync(id, dto, userId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/safa/inspections/{id}")]
    [Authorize(Roles = "SafetyInspector,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> DeleteInspection(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        await safaService.DeleteInspectionAsync(id, userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/safa/inspections/{id}/submit")]
    [Authorize(Roles = "SafetyInspector,TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> SubmitInspection(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await safaService.SubmitInspectionAsync(id, userId, cancellationToken);
        return Ok(result);
    }

    // Defect endpoints
    [HttpPost("api/safa/defects/{id}/take-action")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> TakeCorrectiveAction(Guid id, [FromBody] TakeCorrectiveActionDto dto, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await safaService.TakeCorrectiveActionAsync(id, dto, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("api/safa/defects/{id}/status")]
    [Authorize(Roles = "TeamLeader,Manager,Director,SystemAdmin")]
    public async Task<IActionResult> UpdateDefectStatus(Guid id, [FromBody] UpdateDefectStatusDto dto, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await safaService.UpdateDefectStatusAsync(id, dto, userId, cancellationToken);
        return Ok(result);
    }

    // Dashboard endpoint
    [HttpGet("api/safa/dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await safaService.GetDashboardAsync(userId, startDate, endDate, cancellationToken);
        return Ok(result);
    }
}
