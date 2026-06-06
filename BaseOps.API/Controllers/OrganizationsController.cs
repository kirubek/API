using BaseOps.API.Models;
using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
public sealed class OrganizationsController(BaseOpsDbContext dbContext, IUserScopeResolver scopeResolver) : ControllerBase
{
    [HttpGet("api/v1/organizations/sections")]
    [HttpGet("api/management/sections")]
    public async Task<ActionResult<PaginatedResult<object>>> Sections([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var total = await dbContext.Sections.CountAsync(ct);
        var items = await dbContext.Sections.AsNoTracking().OrderBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize).Select(x => SectionDto(x)).ToArrayAsync(ct);
        return Ok(ApiResults.Page<object>(items, total, page, pageSize));
    }

    [HttpPost("api/v1/organizations/sections")]
    [HttpPost("api/management/sections")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<object>> CreateSection([FromBody] SectionRequest request, CancellationToken ct)
    {
        var section = new Section { Code = request.SectionCode ?? request.Code ?? request.SectionName ?? request.Name ?? "SECTION", Name = request.SectionName ?? request.Name ?? request.SectionCode ?? request.Code ?? "Section" };
        dbContext.Sections.Add(section);
        await dbContext.SaveChangesAsync(ct);
        return Created($"/api/v1/organizations/sections/{section.Id}", SectionDto(section));
    }

    [HttpGet("api/v1/organizations/sections/{id:guid}")]
    [HttpGet("api/management/sections/{id:guid}")]
    public async Task<ActionResult<object>> GetSection(Guid id, CancellationToken ct)
    {
        var section = await dbContext.Sections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return section is null ? NotFound() : Ok(SectionDto(section));
    }

    [HttpPut("api/v1/organizations/sections/{id:guid}")]
    [HttpPut("api/management/sections/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<object>> UpdateSection(Guid id, [FromBody] SectionRequest request, CancellationToken ct)
    {
        var section = await dbContext.Sections.FindAsync([id], ct);
        if (section is null) return NotFound();
        section.Code = request.SectionCode ?? request.Code ?? section.Code;
        section.Name = request.SectionName ?? request.Name ?? section.Name;
        section.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);
        return Ok(SectionDto(section));
    }

    [HttpDelete("api/v1/organizations/sections/{id:guid}")]
    [HttpDelete("api/management/sections/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> DeleteSection(Guid id, CancellationToken ct)
    {
        var section = await dbContext.Sections.FindAsync([id], ct);
        if (section is null) return NotFound();
        dbContext.Sections.Remove(section);
        await dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("api/v1/organizations/hangars")]
    [HttpGet("api/management/hangars")]
    public async Task<ActionResult<PaginatedResult<object>>> Hangars([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] Guid? sectionId = null, CancellationToken ct = default)
    {
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        var query = dbContext.Hangars.AsNoTracking();
        
        // Filter by sectionId if provided
        if (sectionId.HasValue)
        {
            query = query.Where(h => h.SectionId == sectionId.Value);
        }
        // Filter by section for Managers
        else if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            var user = await dbContext.Users.FindAsync([userGuid], ct);
            if (user is not null)
            {
                var userScope = scopeResolver.Resolve(user);
                if (user.Role == UserRole.Manager && userScope.SectionId.HasValue)
                {
                    query = query.Where(h => h.SectionId == userScope.SectionId.Value);
                }
            }
        }
        
        var total = await query.CountAsync(ct);
        var items = await query.Include(x => x.Section).OrderBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize).Select(x => HangarDto(x)).ToArrayAsync(ct);
        return Ok(ApiResults.Page<object>(items, total, page, pageSize));
    }

    [HttpPost("api/v1/organizations/hangars")]
    [HttpPost("api/management/hangars")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<object>> CreateHangar([FromBody] HangarRequest request, CancellationToken ct)
    {
        var hangar = new Hangar { Code = request.HangarCode ?? request.Code ?? request.HangarName ?? request.Name ?? "HANGAR", Name = request.HangarName ?? request.Name ?? request.HangarCode ?? request.Code ?? "Hangar", SectionId = request.SectionId };
        dbContext.Hangars.Add(hangar);
        await dbContext.SaveChangesAsync(ct);
        return Created($"/api/v1/organizations/hangars/{hangar.Id}", HangarDto(hangar));
    }

    [HttpGet("api/v1/organizations/hangars/{id:guid}")]
    [HttpGet("api/management/hangars/{id:guid}")]
    public async Task<ActionResult<object>> GetHangar(Guid id, CancellationToken ct)
    {
        var hangar = await dbContext.Hangars.AsNoTracking().Include(x => x.Section).FirstOrDefaultAsync(x => x.Id == id, ct);
        return hangar is null ? NotFound() : Ok(HangarDto(hangar));
    }

    [HttpPut("api/v1/organizations/hangars/{id:guid}")]
    [HttpPut("api/management/hangars/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<object>> UpdateHangar(Guid id, [FromBody] HangarRequest request, CancellationToken ct)
    {
        var hangar = await dbContext.Hangars.Include(x => x.Section).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (hangar is null) return NotFound();
        hangar.Code = request.HangarCode ?? request.Code ?? hangar.Code;
        hangar.Name = request.HangarName ?? request.Name ?? hangar.Name;
        hangar.SectionId = request.SectionId;
        hangar.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);
        return Ok(HangarDto(hangar));
    }

    [HttpDelete("api/v1/organizations/hangars/{id:guid}")]
    [HttpDelete("api/management/hangars/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> DeleteHangar(Guid id, CancellationToken ct)
    {
        var hangar = await dbContext.Hangars.FindAsync([id], ct);
        if (hangar is null) return NotFound();
        dbContext.Hangars.Remove(hangar);
        await dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("api/v1/organizations/shops")]
    [HttpGet("api/management/shops")]
    public async Task<ActionResult<PaginatedResult<object>>> Shops([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        var query = dbContext.Shops.AsNoTracking();
        
        // Filter by section for Managers
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            var user = await dbContext.Users.FindAsync([userGuid], ct);
            if (user is not null)
            {
                var userScope = scopeResolver.Resolve(user);
                if (user.Role == UserRole.Manager && userScope.SectionId.HasValue)
                {
                    query = query.Where(s => s.SectionId == userScope.SectionId.Value);
                }
            }
        }
        
        var total = await query.CountAsync(ct);
        var items = await query.Include(x => x.Section).OrderBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize).Select(x => ShopDto(x)).ToArrayAsync(ct);
        return Ok(ApiResults.Page<object>(items, total, page, pageSize));
    }

    [HttpPost("api/v1/organizations/shops")]
    [HttpPost("api/management/shops")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<object>> CreateShop([FromBody] ShopRequest request, CancellationToken ct)
    {
        var shop = new Shop { Code = request.ShopCode ?? request.Code ?? request.ShopName ?? request.Name ?? "SHOP", Name = request.ShopName ?? request.Name ?? request.ShopCode ?? request.Code ?? "Shop", SectionId = request.SectionId };
        dbContext.Shops.Add(shop);
        await dbContext.SaveChangesAsync(ct);
        return Created($"/api/v1/organizations/shops/{shop.Id}", ShopDto(shop));
    }

    [HttpGet("api/v1/organizations/shops/{id:guid}")]
    [HttpGet("api/management/shops/{id:guid}")]
    public async Task<ActionResult<object>> GetShop(Guid id, CancellationToken ct)
    {
        var shop = await dbContext.Shops.AsNoTracking().Include(x => x.Section).FirstOrDefaultAsync(x => x.Id == id, ct);
        return shop is null ? NotFound() : Ok(ShopDto(shop));
    }

    [HttpPut("api/v1/organizations/shops/{id:guid}")]
    [HttpPut("api/management/shops/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<object>> UpdateShop(Guid id, [FromBody] ShopRequest request, CancellationToken ct)
    {
        var shop = await dbContext.Shops.Include(x => x.Section).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (shop is null) return NotFound();
        shop.Code = request.ShopCode ?? request.Code ?? shop.Code;
        shop.Name = request.ShopName ?? request.Name ?? shop.Name;
        shop.SectionId = request.SectionId;
        shop.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);
        return Ok(ShopDto(shop));
    }

    [HttpDelete("api/v1/organizations/shops/{id:guid}")]
    [HttpDelete("api/management/shops/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> DeleteShop(Guid id, CancellationToken ct)
    {
        var shop = await dbContext.Shops.FindAsync([id], ct);
        if (shop is null) return NotFound();
        dbContext.Shops.Remove(shop);
        await dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    private static object SectionDto(Section x) => new { id = x.Id, sectionCode = x.Code, sectionName = x.Name, name = x.Name, code = x.Code, isActive = true, createdAt = x.CreatedAt, updatedAt = x.UpdatedAt ?? x.CreatedAt };
    private static object HangarDto(Hangar x) => new { id = x.Id, hangarCode = x.Code, hangarName = x.Name, name = x.Name, code = x.Code, sectionId = x.SectionId, sectionName = x.Section != null ? x.Section.Name : null, isActive = true, createdAt = x.CreatedAt, updatedAt = x.UpdatedAt ?? x.CreatedAt };
    private static object ShopDto(Shop x) => new { id = x.Id, shopCode = x.Code, shopName = x.Name, name = x.Name, code = x.Code, sectionId = x.SectionId, sectionName = x.Section != null ? x.Section.Name : null, isActive = true, createdAt = x.CreatedAt, updatedAt = x.UpdatedAt ?? x.CreatedAt };

    public sealed record SectionRequest(string? SectionCode, string? SectionName, string? Code, string? Name);
    public sealed record HangarRequest(string? HangarCode, string? HangarName, string? Code, string? Name, Guid SectionId);
    public sealed record ShopRequest(string? ShopCode, string? ShopName, string? Code, string? Name, Guid SectionId);
}

