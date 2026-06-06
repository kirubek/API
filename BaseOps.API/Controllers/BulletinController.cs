using BaseOps.API.Models;
using BaseOps.Application.Bulletins;
using BaseOps.Application.Bulletins.DTOs;
using BaseOps.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaseOps.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BulletinController(IBulletinService bulletinService, IAttachmentSecurityService attachmentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBulletins(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BulletinCategory? category = null,
        [FromQuery] BulletinPriority? priority = null,
        [FromQuery] bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var (items, totalCount) = await bulletinService.GetBulletinsAsync(
            userId.Value,
            pageNumber,
            pageSize,
            category,
            priority,
            unreadOnly,
            cancellationToken);

        return Ok(ApiResults.Page(items, totalCount, pageNumber, pageSize));
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardBulletins(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var bulletins = await bulletinService.GetDashboardBulletinsAsync(userId.Value, cancellationToken);
        return Ok(bulletins);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBulletin(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var bulletin = await bulletinService.GetBulletinAsync(id, userId.Value, cancellationToken);
        if (bulletin is null) return NotFound();

        return Ok(bulletin);
    }

    [HttpPost]
    [Authorize(Roles = "Director,Manager,TeamLeader")]
    public async Task<IActionResult> CreateBulletin([FromBody] CreateBulletinDto dto, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var bulletin = await bulletinService.CreateBulletinAsync(dto, userId.Value, cancellationToken);
        return Ok(bulletin);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director,Manager,TeamLeader")]
    public async Task<IActionResult> UpdateBulletin(Guid id, [FromBody] UpdateBulletinDto dto, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var bulletin = await bulletinService.UpdateBulletinAsync(id, dto, userId.Value, cancellationToken);
        if (bulletin is null) return NotFound();

        return Ok(bulletin);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director,Manager,TeamLeader")]
    public async Task<IActionResult> DeleteBulletin(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        await bulletinService.DeleteBulletinAsync(id, userId.Value, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        await bulletinService.MarkAsReadAsync(id, userId.Value, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        await bulletinService.MarkAllAsReadAsync(userId.Value, cancellationToken);
        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var count = await bulletinService.GetUnreadCountAsync(userId.Value, cancellationToken);
        return Ok(new { count });
    }

    [HttpGet("{id:guid}/analytics")]
    [Authorize(Roles = "Director,Manager,TeamLeader")]
    public async Task<IActionResult> GetAnalytics(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var analytics = await bulletinService.GetAnalyticsAsync(id, userId.Value, cancellationToken);
        if (analytics is null) return NotFound();

        return Ok(analytics);
    }

    [HttpGet("my-analytics")]
    [Authorize(Roles = "Director,Manager,TeamLeader")]
    public async Task<IActionResult> GetMyBulletinsAnalytics(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var analytics = await bulletinService.GetMyBulletinsAnalyticsAsync(userId.Value, cancellationToken);
        return Ok(analytics);
    }

    [HttpGet("archive")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> GetArchive(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var bulletins = await bulletinService.GetArchiveAsync(userId.Value, cancellationToken);
        return Ok(bulletins);
    }

    [HttpPost("{id:guid}/attachment")]
    [Authorize(Roles = "Director,Manager,TeamLeader")]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        // Read file content
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileContent = memoryStream.ToArray();

        // Store attachment securely
        var (generatedFileName, storagePath) = await attachmentService.StoreAttachmentAsync(
            file.FileName,
            file.ContentType,
            fileContent,
            userId.Value,
            cancellationToken);

        // Save attachment to database
        var attachment = await bulletinService.AddAttachmentAsync(id, generatedFileName, file.FileName, file.ContentType, fileContent.Length, storagePath, userId.Value, cancellationToken);
        
        return Ok(new
        {
            id = attachment.Id,
            originalFileName = file.FileName,
            generatedFileName = generatedFileName,
            contentType = file.ContentType,
            fileSize = file.Length
        });
    }

    [HttpGet("{id:guid}/attachment/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var fileContent = await bulletinService.GetAttachmentContentAsync(attachmentId, cancellationToken);
        if (fileContent == null) return NotFound();

        var attachment = await bulletinService.GetBulletinAsync(id, userId.Value, cancellationToken);
        var attachmentInfo = attachment?.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        
        if (attachmentInfo == null) return NotFound();

        return File(fileContent, attachmentInfo.ContentType, attachmentInfo.OriginalFileName);
    }
}
