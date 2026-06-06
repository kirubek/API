using BaseOps.API.Models;
using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Authentication;
using BaseOps.Infrastructure.Data;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace BaseOps.API.Controllers;

[ApiController]
[Authorize]
public sealed class UsersController(BaseOpsDbContext dbContext, IPasswordHasher passwordHasher, IUserScopeResolver scopeResolver) : ControllerBase
{
    [HttpGet("api/v1/users")]
    [HttpGet("api/management/users")]
    public async Task<ActionResult<PaginatedResult<object>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? sectionId = null, [FromQuery] string? role = null, [FromQuery] bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        var query = dbContext.Users.AsNoTracking();
        
        // Apply filters from query parameters
        if (sectionId.HasValue)
        {
            System.Console.WriteLine($"Filtering by sectionId: {sectionId.Value}");
            query = query.Where(x => x.SectionId == sectionId.Value);
        }
        
        if (!string.IsNullOrEmpty(role))
        {
            var parsedRole = ParseRole(role);
            System.Console.WriteLine($"Filtering by role: {parsedRole}");
            query = query.Where(x => x.Role == parsedRole);
        }
        
        if (isActive.HasValue)
        {
            System.Console.WriteLine($"Filtering by isActive: {isActive.Value}");
            query = query.Where(x => x.IsActive == isActive.Value);
        }
        
        // Filter by section for Managers (unless a specific section filter is already applied)
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            var user = await dbContext.Users.FindAsync([userGuid], cancellationToken);
            if (user is not null)
            {
                var userScope = scopeResolver.Resolve(user);
                if (user.Role == UserRole.Manager && userScope.SectionId.HasValue && !sectionId.HasValue)
                {
                    System.Console.WriteLine($"Manager filtering by own section: {userScope.SectionId.Value}");
                    query = query.Where(x => x.SectionId == userScope.SectionId.Value);
                }
            }
        }
        
        var total = await query.CountAsync(cancellationToken);
        System.Console.WriteLine($"Total users after filtering: {total}");
        var users = await query.Include(x => x.Section).Include(x => x.Hangar).Include(x => x.Shop).OrderBy(x => x.EmployeeId).Skip((page - 1) * pageSize).Take(pageSize).Select(x => ToDto(x)).ToArrayAsync(cancellationToken);
        return Ok(ApiResults.Page<object>(users, total, page, pageSize));
    }

    [HttpGet("api/v1/users/{id:guid}")]
    [HttpGet("api/management/users/{id:guid}")]
    [HttpGet("api/users/{id:guid}")]
    public async Task<ActionResult<object>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().Include(x => x.Section).Include(x => x.Hangar).Include(x => x.Shop).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(ToDto(user));
    }

    [HttpGet("api/users/current")]
    [HttpGet("api/auth/me")]
    public async Task<ActionResult<object>> Current(CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        if (id is null) return Unauthorized();
        var user = await dbContext.Users.AsNoTracking().Include(x => x.Section).Include(x => x.Hangar).Include(x => x.Shop).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return user is null ? Unauthorized() : Ok(ToDto(user));
    }

    [HttpGet("api/user/team-members")]
    public async Task<ActionResult<object>> GetTeamMembers(CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        if (id is null) return Unauthorized();
        
        var currentUser = await dbContext.Users.AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        
        if (currentUser is null) return Unauthorized();

        // Query team members based on user's role and workspace
        var query = dbContext.Users.AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.Hangar)
            .Include(x => x.Shop)
            .Where(x => x.IsActive);

        // Filter based on user's role
        if (currentUser.Role == UserRole.Employee)
        {
            // Employees see only themselves + employees with same ReportsToUserId (same team leader)
            if (currentUser.ReportsToUserId.HasValue)
            {
                query = query.Where(x => x.Id == currentUser.Id || (x.Role == UserRole.Employee && x.ReportsToUserId == currentUser.ReportsToUserId));
                Console.WriteLine($"[UsersController] Employee {currentUser.FullName} ({currentUser.Id}) filtering by ReportsToUserId == {currentUser.ReportsToUserId}");
            }
            else
            {
                // Fallback: Employees see only employees in their same hangar/shop
                query = query.Where(x => x.Role == UserRole.Employee);
                if (currentUser.HangarId.HasValue)
                    query = query.Where(x => x.HangarId == currentUser.HangarId);
                else if (currentUser.ShopId.HasValue)
                    query = query.Where(x => x.ShopId == currentUser.ShopId);
                Console.WriteLine($"[UsersController] Employee {currentUser.FullName} ({currentUser.Id}) has no ReportsToUserId, filtering by hangar/shop");
            }
        }
        else if (currentUser.Role == UserRole.TeamLeader)
        {
            // Team leaders see themselves + employees that report to them
            query = query.Where(x => (x.Role == UserRole.Employee && x.ReportsToUserId == currentUser.Id) || x.Id == currentUser.Id);
            Console.WriteLine($"[UsersController] Team Leader {currentUser.FullName} ({currentUser.Id}) filtering by ReportsToUserId == {currentUser.Id}");
        }
        else if (currentUser.Role == UserRole.Manager)
        {
            // Managers see employees in their section
            if (currentUser.SectionId.HasValue)
                query = query.Where(x => x.SectionId == currentUser.SectionId);
        }
        // Directors and Admins see all employees (no filter)

        var teamMembers = await query
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                id = x.Id,
                employeeId = x.EmployeeId,
                firstName = x.FullName.Split(' ').FirstOrDefault() ?? x.FullName,
                lastName = x.FullName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                fullName = x.FullName,
                email = x.Email,
                role = x.Role == UserRole.SystemAdmin ? "SystemAdministrator" : x.Role == UserRole.SafetyInspector ? "SafaInspector" : x.Role.ToString(),
                position = x.Position,
                sectionId = x.SectionId,
                sectionName = x.Section != null ? x.Section.Name : null,
                hangarId = x.HangarId,
                hangarName = x.Hangar != null ? x.Hangar.Name : null,
                shopId = x.ShopId,
                shopName = x.Shop != null ? x.Shop.Name : null,
                teamLeaderId = x.HangarId == currentUser.HangarId || x.ShopId == currentUser.ShopId ? currentUser.Id.ToString() : null,
                isActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(teamMembers);
    }

    [HttpPost("api/v1/users")]
    [HttpPost("api/management/users")]
    [HttpPost("api/admin/create-user")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<object>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        System.Console.WriteLine($"CreateUser request: EmployeeId={request.EmployeeId}, FullName={request.FullName}, Email={request.Email}, Role={request.Role}, SectionId={request.SectionId}, HangarId={request.HangarId}, ShopId={request.ShopId}");
        
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        // Check if email already exists (if provided)
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
            if (existingUser is not null)
            {
                return BadRequest(new { message = "A user with this email already exists." });
            }
        }
        
        // Check if employeeId already exists
        var existingEmployee = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId, cancellationToken);
        if (existingEmployee is not null)
        {
            return BadRequest(new { message = "A user with this Employee ID already exists." });
        }
        
        // RBAC validation for Managers
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            var currentUser = await dbContext.Users.FindAsync([userGuid], cancellationToken);
            if (currentUser is not null && currentUser.Role == UserRole.Manager)
            {
                var userScope = scopeResolver.Resolve(currentUser);
                // Managers can only create users for their own section
                if (userScope.SectionId.HasValue)
                {
                    if (request.SectionId.HasValue && request.SectionId.Value != userScope.SectionId.Value)
                    {
                        return BadRequest(new { message = "Managers can only create users for their own section." });
                    }
                    // Force the section to be the manager's section
                    request = request with { SectionId = userScope.SectionId.Value };
                }
            }
        }
        
        var role = ParseRole(request.Role);
        var user = new ApplicationUser
        {
            EmployeeId = request.EmployeeId,
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(string.IsNullOrWhiteSpace(request.Password) ? "ChangeMe@123" : request.Password),
            Role = role,
            SectionId = request.SectionId,
            HangarId = request.HangarId,
            ShopId = request.ShopId,
            ReportsToUserId = request.TeamLeaderId,
            MustChangePassword = true,
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/users/{user.Id}", ToDto(user));
    }

    [HttpGet("api/v1/users/assignments")]
    [HttpGet("api/management/users/assignments")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<PaginatedResult<object>>> GetAssignments([FromQuery] Guid? userId, [FromQuery] Guid? workspaceId, [FromQuery] string? assignmentType, [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UserWorkspaceAssignments.AsNoTracking().Include(x => x.User).Include(x => x.Section).Include(x => x.Hangar).Include(x => x.Shop).AsQueryable();
        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);
        if (workspaceId.HasValue) query = query.Where(x => x.WorkspaceId == workspaceId.Value);
        if (!string.IsNullOrWhiteSpace(assignmentType)) query = query.Where(x => x.AssignmentType == assignmentType);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => AssignmentDto(x)).ToArrayAsync(cancellationToken);
        return Ok(ApiResults.Page<object>(items, total, page, pageSize));
    }

    [HttpPost("api/v1/users/assignments")]
    [HttpPost("api/management/users/assignments")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<object>> CreateAssignment([FromBody] CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var workspace = await ResolveWorkspaceAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null) return BadRequest(new { message = "Workspace was not found." });

        var assignment = new UserWorkspaceAssignment
        {
            UserId = request.UserId,
            WorkspaceId = request.WorkspaceId,
            WorkspaceType = workspace.WorkspaceType,
            SectionId = workspace.SectionId,
            HangarId = workspace.HangarId,
            ShopId = workspace.ShopId,
            AssignmentType = request.AssignmentType,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            IsActive = true
        };

        dbContext.UserWorkspaceAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/users/assignments/{assignment.Id}", AssignmentDto(assignment));
    }

    [HttpPut("api/v1/users/assignments/{id:guid}")]
    [HttpPut("api/management/users/assignments/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<object>> UpdateAssignment(Guid id, [FromBody] UpdateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.UserWorkspaceAssignments.FindAsync([id], cancellationToken);
        if (assignment is null) return NotFound();
        if (request.AssignmentType is not null) assignment.AssignmentType = request.AssignmentType;
        if (request.StartAt.HasValue) assignment.StartAt = request.StartAt.Value;
        if (request.EndAt.HasValue) assignment.EndAt = request.EndAt;
        if (request.IsActive.HasValue) assignment.IsActive = request.IsActive.Value;
        assignment.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(AssignmentDto(assignment));
    }

    [HttpDelete("api/v1/users/assignments/{id:guid}")]
    [HttpDelete("api/management/users/assignments/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> DeleteAssignment(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.UserWorkspaceAssignments.FindAsync([id], cancellationToken);
        if (assignment is null) return NotFound();
        assignment.IsActive = false;
        assignment.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("api/v1/users/{id:guid}")]
    [HttpPut("api/management/users/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<ActionResult<object>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        System.Console.WriteLine($"[UsersController] Update user {id}, Position in request: {request.Position}");

        var user = await dbContext.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();

        System.Console.WriteLine($"[UsersController] Current user position: {user.Position}");

        // RBAC validation for Managers
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            var currentUser = await dbContext.Users.FindAsync([userGuid], cancellationToken);
            if (currentUser is not null && currentUser.Role == UserRole.Manager)
            {
                var userScope = scopeResolver.Resolve(currentUser);
                // Managers can only edit users in their own section
                if (userScope.SectionId.HasValue)
                {
                    if (user.SectionId.HasValue && user.SectionId.Value != userScope.SectionId.Value)
                    {
                        return BadRequest(new { message = "Managers can only edit users in their own section." });
                    }
                    // Managers can only assign TeamLeader and Employee roles
                    if (request.Role is not null)
                    {
                        var newRole = ParseRole(request.Role);
                        if (newRole != UserRole.TeamLeader && newRole != UserRole.Employee)
                        {
                            return BadRequest(new { message = "Managers can only assign TeamLeader or Employee roles." });
                        }
                    }
                    // Force the section to be the manager's section if being changed
                    if (request.SectionId.HasValue && request.SectionId.Value != userScope.SectionId.Value)
                    {
                        return BadRequest(new { message = "Managers can only assign users to their own section." });
                    }
                }
            }
        }

        if (request.FullName is not null) user.FullName = request.FullName;
        if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
        if (request.PhoneNumber is not null) user.PhoneNumber = request.PhoneNumber;
        if (request.Position is not null) user.Position = request.Position;
        if (request.Role is not null) user.Role = ParseRole(request.Role);
        if (request.SectionId.HasValue) user.SectionId = request.SectionId;
        if (request.HangarId.HasValue) user.HangarId = request.HangarId;
        if (request.ShopId.HasValue) user.ShopId = request.ShopId;
        if (request.ReportsToUserId.HasValue) user.ReportsToUserId = request.ReportsToUserId;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        System.Console.WriteLine($"[UsersController] After update, user position: {user.Position}");

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(user));
    }

    [HttpPatch("api/v1/users/{id:guid}/deactivate")]
    [HttpPatch("api/management/users/{id:guid}/deactivate")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public Task<ActionResult<object>> Deactivate(Guid id, CancellationToken cancellationToken) => SetActive(id, false, cancellationToken);

    [HttpPatch("api/v1/users/{id:guid}/reactivate")]
    [HttpPatch("api/management/users/{id:guid}/reactivate")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public Task<ActionResult<object>> Reactivate(Guid id, CancellationToken cancellationToken) => SetActive(id, true, cancellationToken);

    [HttpPatch("api/v1/users/{id:guid}/lock")]
    [HttpPatch("api/v1/users/{id:guid}/unlock")]
    [HttpPatch("api/management/users/{id:guid}/lock")]
    [HttpPatch("api/management/users/{id:guid}/unlock")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public IActionResult LockUnlock(Guid id) => Ok(new { success = true, id });

    [HttpDelete("api/v1/users/{id:guid}")]
    [HttpDelete("api/management/users/{id:guid}")]
    [Authorize(Roles = "Manager,Director,SystemAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();
        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult<object>> SetActive(Guid id, bool active, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();
        user.IsActive = active;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(user));
    }

    private static UserRole ParseRole(string role) => role switch
    {
        "SystemAdministrator" => UserRole.SystemAdmin,
        "SafaInspector" => UserRole.SafetyInspector,
        _ when Enum.TryParse<UserRole>(role, true, out var parsed) => parsed,
        _ => UserRole.Employee
    };

    private static object ToDto(ApplicationUser x) => new
    {
        id = x.Id,
        employeeId = x.EmployeeId,
        fullName = x.FullName,
        email = x.Email ?? string.Empty,
        role = x.Role == UserRole.SystemAdmin ? "SystemAdministrator" : x.Role == UserRole.SafetyInspector ? "SafaInspector" : x.Role.ToString(),
        sectionId = x.SectionId,
        sectionName = x.Section != null ? x.Section.Name : null,
        section = x.Section != null ? new { id = x.Section.Id, sectionCode = x.Section.Code, sectionName = x.Section.Name, isActive = true, createdAt = x.Section.CreatedAt, updatedAt = x.Section.UpdatedAt ?? x.Section.CreatedAt } : null,
        hangarId = x.HangarId,
        hangarName = x.Hangar != null ? x.Hangar.Name : null,
        shopId = x.ShopId,
        shopName = x.Shop != null ? x.Shop.Name : null,
        isActive = x.IsActive,
        isLocked = false,
        mustChangePassword = x.MustChangePassword,
        lastLoginAt = x.LastLoginAt,
        createdAt = x.CreatedAt,
        updatedAt = x.UpdatedAt ?? x.CreatedAt,
        capabilities = x.Section != null && x.Section.Name == "Technical Support-Base" ? new[] { "PRODUCTION_PLANNER_OPERATIONS" } : Array.Empty<string>()
    };

    private async Task<WorkspaceResolution?> ResolveWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var hangar = await dbContext.Hangars.AsNoTracking().FirstOrDefaultAsync(x => x.Id == workspaceId, cancellationToken);
        if (hangar is not null) return new WorkspaceResolution("Hangar", hangar.SectionId, hangar.Id, null);
        var shop = await dbContext.Shops.AsNoTracking().FirstOrDefaultAsync(x => x.Id == workspaceId, cancellationToken);
        if (shop is not null) return new WorkspaceResolution("Shop", shop.SectionId, null, shop.Id);
        var section = await dbContext.Sections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == workspaceId, cancellationToken);
        return section is null ? null : new WorkspaceResolution("Section", section.Id, null, null);
    }

    private static object AssignmentDto(UserWorkspaceAssignment x) => new
    {
        id = x.Id,
        userId = x.UserId,
        workspaceId = x.WorkspaceId,
        assignmentType = x.AssignmentType,
        startAt = x.StartAt,
        endAt = x.EndAt,
        isActive = x.IsActive,
        user = x.User is null ? null : ToDto(x.User),
        workspace = x.WorkspaceType switch
        {
            "Hangar" => x.Hangar is null ? null : new { id = x.Hangar.Id, name = x.Hangar.Name, type = x.WorkspaceType, locationCode = x.Hangar.Code },
            "Shop" => x.Shop is null ? null : new { id = x.Shop.Id, name = x.Shop.Name, type = x.WorkspaceType, locationCode = x.Shop.Code },
            _ => x.Section is null ? null : new { id = x.Section.Id, name = x.Section.Name, type = x.WorkspaceType, locationCode = x.Section.Code }
        }
    };

    [HttpGet("api/v1/users/template")]
    [HttpGet("api/management/users/template")]
    public IActionResult GetTemplate([FromQuery] string format = "csv")
    {
        if (format.ToLower() == "excel" || format.ToLower() == "xlsx")
        {
            return GetExcelTemplate();
        }
        return GetCsvTemplate();
    }

    private IActionResult GetCsvTemplate()
    {
        var csv = new StringBuilder();
        csv.AppendLine("EmployeeId,FullName,Email,Role,SectionCode,HangarCode,ShopCode,Password");
        csv.AppendLine("EMP001,John Doe,john.doe@example.com,Employee,ACM,B737-ACM,,ChangeMe@123");
        csv.AppendLine("EMP002,Jane Smith,jane.smith@example.com,TeamLeader,ACM,B737-ACM,,ChangeMe@123");
        csv.AppendLine("MGR010,Manager Name,manager@example.com,Manager,ASM,B777-ASM,,ChangeMe@123");
        
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "user_import_template.csv");
    }

    private IActionResult GetExcelTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Users");
        
        // Add headers
        worksheet.Cell("A1").Value = "EmployeeId";
        worksheet.Cell("B1").Value = "FullName";
        worksheet.Cell("C1").Value = "Position";
        worksheet.Cell("D1").Value = "Email";
        worksheet.Cell("E1").Value = "Role";
        worksheet.Cell("F1").Value = "Password";
        
        // Style headers
        var headerRange = worksheet.Range("A1:F1");
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        
        // Add example data
        worksheet.Cell("A2").Value = "EMP001";
        worksheet.Cell("B2").Value = "John Doe";
        worksheet.Cell("C2").Value = "Mechanic";
        worksheet.Cell("D2").Value = "john.doe@example.com";
        worksheet.Cell("E2").Value = "Employee";
        worksheet.Cell("F2").Value = "ChangeMe@123";
        
        worksheet.Cell("A3").Value = "EMP002";
        worksheet.Cell("B3").Value = "Jane Smith";
        worksheet.Cell("C3").Value = "Senior Mechanic";
        worksheet.Cell("D3").Value = "jane.smith@example.com";
        worksheet.Cell("E3").Value = "TeamLeader";
        worksheet.Cell("F3").Value = "ChangeMe@123";
        
        worksheet.Cell("A4").Value = "MGR010";
        worksheet.Cell("B4").Value = "Manager Name";
        worksheet.Cell("C4").Value = "Section Manager";
        worksheet.Cell("D4").Value = "manager@example.com";
        worksheet.Cell("E4").Value = "Manager";
        worksheet.Cell("F4").Value = "ChangeMe@123";
        
        // Add instructions sheet
        var instructionsSheet = workbook.Worksheets.Add("Instructions");
        instructionsSheet.Cell("A1").Value = "Bulk User Import Instructions";
        instructionsSheet.Cell("A1").Style.Font.Bold = true;
        instructionsSheet.Cell("A1").Style.Font.FontSize = 14;
        
        instructionsSheet.Cell("A3").Value = "Required Fields:";
        instructionsSheet.Cell("A3").Style.Font.Bold = true;
        instructionsSheet.Cell("A4").Value = "• EmployeeId - Unique employee identifier";
        instructionsSheet.Cell("A5").Value = "• FullName - Full name of the employee";
        instructionsSheet.Cell("A6").Value = "• Position - Job position/title";
        instructionsSheet.Cell("A7").Value = "• Role - One of: Employee, TeamLeader, Manager, Director, SystemAdmin, SafaInspector";
        
        instructionsSheet.Cell("A9").Value = "Optional Fields:";
        instructionsSheet.Cell("A9").Style.Font.Bold = true;
        instructionsSheet.Cell("A10").Value = "• Email - Valid email address (optional but recommended)";
        instructionsSheet.Cell("A11").Value = "• Password - Password (defaults to ChangeMe@123 if not provided)";
        
        instructionsSheet.Cell("A13").Value = "Important Notes:";
        instructionsSheet.Cell("A13").Style.Font.Bold = true;
        instructionsSheet.Cell("A14").Value = "• Section, Hangar, and Shop are selected in the bulk import UI";
        instructionsSheet.Cell("A15").Value = "• Do not include SectionCode, HangarCode, or ShopCode in the CSV";
        instructionsSheet.Cell("A16").Value = "• All imported users will be assigned to the selected section/hangar/shop";
        
        instructionsSheet.Cell("A18").Value = "Manager Restrictions:";
        instructionsSheet.Cell("A18").Style.Font.Bold = true;
        instructionsSheet.Cell("A19").Value = "• Managers can only create TeamLeaders and Employees";
        instructionsSheet.Cell("A20").Value = "• Managers can only create users for their own section";
        instructionsSheet.Cell("A21").Value = "• Section will be auto-assigned if not specified";
        
        instructionsSheet.Cell("A23").Value = "Valid Section Codes:";
        instructionsSheet.Cell("A23").Style.Font.Bold = true;
        instructionsSheet.Cell("A24").Value = "ACM - Aircraft Cabin Maintenance";
        instructionsSheet.Cell("A25").Value = "ASM - Aircraft Structure Maintenance";
        instructionsSheet.Cell("A26").Value = "AAS - Aircraft Avionics";
        instructionsSheet.Cell("A27").Value = "PS - Paint Service";
        instructionsSheet.Cell("A28").Value = "SM777 - B777/A350 Hangar";
        instructionsSheet.Cell("A29").Value = "SM787 - B787/B767 Hangar";
        instructionsSheet.Cell("A30").Value = "SM737 - B737/Q400 Hangar";
        instructionsSheet.Cell("A31").Value = "TSB - Technical Support Base";
        instructionsSheet.Cell("A32").Value = "SMQ400 - Q400 Hangar";
        
        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        instructionsSheet.Columns().AdjustToContents();
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "user_import_template.xlsx");
    }

    [HttpPost("api/v1/users/bulk-import")]
    [HttpPost("api/management/users/bulk-import")]
    public async Task<ActionResult<object>> BulkImport(IFormFile file, string? sectionId, string? hangarId, string? shopId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var currentUser = await dbContext.Users.FindAsync([userGuid], cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var userScope = scopeResolver.Resolve(currentUser);

        // RBAC validation
        if (currentUser.Role != UserRole.SystemAdmin && currentUser.Role != UserRole.Director && currentUser.Role != UserRole.Manager)
        {
            return Forbid("Only System Admin, Director, and Manager can bulk import users.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided.");
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only CSV files are supported.");
        }

        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvHelper.CsvReader(reader, CultureInfo.InvariantCulture);
        
        var records = csv.GetRecords<BulkImportUserRecord>().ToList();
        
        if (records.Count == 0)
        {
            return BadRequest("No valid records found in the file.");
        }

        var results = new BulkImportResult
        {
            TotalRecords = records.Count,
            SuccessCount = 0,
            FailedCount = 0,
            Errors = new List<string>()
        };

        foreach (var record in records)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(record.EmployeeId) || string.IsNullOrWhiteSpace(record.FullName) || string.IsNullOrWhiteSpace(record.Position))
                {
                    results.FailedCount++;
                    results.Errors.Add($"Row: Missing required fields (EmployeeId, FullName, Position)");
                    continue;
                }

                // Check for duplicate employee ID
                if (await dbContext.Users.AnyAsync(x => x.EmployeeId == record.EmployeeId, cancellationToken))
                {
                    results.FailedCount++;
                    results.Errors.Add($"Row {record.EmployeeId}: Employee ID already exists");
                    continue;
                }

                // Parse role
                var role = ParseRole(record.Role);

                // RBAC: Managers can only create TeamLeaders and Employees
                if (currentUser.Role == UserRole.Manager)
                {
                    if (role != UserRole.TeamLeader && role != UserRole.Employee)
                    {
                        results.FailedCount++;
                        results.Errors.Add($"Row {record.EmployeeId}: Managers can only create TeamLeaders and Employees");
                        continue;
                    }
                }

                // Resolve section by code or use provided sectionId parameter
                Guid? resolvedSectionId = null;
                Guid? resolvedHangarId = null;
                Guid? resolvedShopId = null;

                if (!string.IsNullOrWhiteSpace(sectionId) && Guid.TryParse(sectionId, out var providedSectionId))
                {
                    // Use the sectionId from the bulk import request
                    var section = await dbContext.Sections.FirstOrDefaultAsync(x => x.Id == providedSectionId, cancellationToken);
                    if (section is null)
                    {
                        results.FailedCount++;
                        results.Errors.Add($"Row {record.EmployeeId}: Provided section ID not found");
                        continue;
                    }

                    // RBAC: Managers can only create users for their own section
                    if (currentUser.Role == UserRole.Manager && userScope.SectionId.HasValue && section.Id != userScope.SectionId.Value)
                    {
                        results.FailedCount++;
                        results.Errors.Add($"Row {record.EmployeeId}: Managers can only create users for their own section");
                        continue;
                    }

                    resolvedSectionId = section.Id;

                    // Use provided hangarId if available
                    if (!string.IsNullOrWhiteSpace(hangarId) && Guid.TryParse(hangarId, out var providedHangarId))
                    {
                        var hangar = await dbContext.Hangars.FirstOrDefaultAsync(x => x.Id == providedHangarId, cancellationToken);
                        if (hangar is null || hangar.SectionId != resolvedSectionId.Value)
                        {
                            results.FailedCount++;
                            results.Errors.Add($"Row {record.EmployeeId}: Provided hangar ID not found or not in selected section");
                            continue;
                        }
                        resolvedHangarId = hangar.Id;
                    }

                    // Use provided shopId if available
                    if (!string.IsNullOrWhiteSpace(shopId) && Guid.TryParse(shopId, out var providedShopId))
                    {
                        var shop = await dbContext.Shops.FirstOrDefaultAsync(x => x.Id == providedShopId, cancellationToken);
                        if (shop is null || shop.SectionId != resolvedSectionId.Value)
                        {
                            results.FailedCount++;
                            results.Errors.Add($"Row {record.EmployeeId}: Provided shop ID not found or not in selected section");
                            continue;
                        }
                        resolvedShopId = shop.Id;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(record.SectionCode))
                {
                    // Fallback to SectionCode from CSV if sectionId parameter not provided
                    var section = await dbContext.Sections.FirstOrDefaultAsync(x => x.Code == record.SectionCode, cancellationToken);
                    if (section is null)
                    {
                        results.FailedCount++;
                        results.Errors.Add($"Row {record.EmployeeId}: Section code '{record.SectionCode}' not found");
                        continue;
                    }

                    // RBAC: Managers can only create users for their own section
                    if (currentUser.Role == UserRole.Manager && userScope.SectionId.HasValue && section.Id != userScope.SectionId.Value)
                    {
                        results.FailedCount++;
                        results.Errors.Add($"Row {record.EmployeeId}: Managers can only create users for their own section");
                        continue;
                    }

                    resolvedSectionId = section.Id;

                    // Resolve hangar by code
                    if (!string.IsNullOrWhiteSpace(record.HangarCode))
                    {
                        var hangar = await dbContext.Hangars.FirstOrDefaultAsync(x => x.Code == record.HangarCode, cancellationToken);
                        if (hangar is null)
                        {
                            results.FailedCount++;
                            results.Errors.Add($"Row {record.EmployeeId}: Hangar code '{record.HangarCode}' not found");
                            continue;
                        }
                        resolvedHangarId = hangar.Id;
                    }

                    // Resolve shop by code
                    if (!string.IsNullOrWhiteSpace(record.ShopCode))
                    {
                        var shop = await dbContext.Shops.FirstOrDefaultAsync(x => x.Code == record.ShopCode, cancellationToken);
                        if (shop is null)
                        {
                            results.FailedCount++;
                            results.Errors.Add($"Row {record.EmployeeId}: Shop code '{record.ShopCode}' not found");
                            continue;
                        }
                        resolvedShopId = shop.Id;
                    }
                }
                else if (currentUser.Role == UserRole.Manager)
                {
                    // Force manager's section if not specified
                    resolvedSectionId = userScope.SectionId;
                }

                var user = new ApplicationUser
                {
                    EmployeeId = record.EmployeeId,
                    FullName = record.FullName,
                    Email = record.Email,
                    Position = record.Position,
                    PasswordHash = passwordHasher.Hash(string.IsNullOrWhiteSpace(record.Password) ? "ChangeMe@123" : record.Password),
                    Role = role,
                    SectionId = resolvedSectionId,
                    HangarId = resolvedHangarId,
                    ShopId = resolvedShopId,
                    MustChangePassword = true,
                    IsActive = true
                };

                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync(cancellationToken);
                results.SuccessCount++;
            }
            catch (Exception ex)
            {
                results.FailedCount++;
                results.Errors.Add($"Row {record.EmployeeId}: {ex.Message}");
            }
        }

        return Ok(results);
    }

    public sealed record CreateUserRequest(string EmployeeId, string FullName, string? Email, string Role, Guid? SectionId, Guid? HangarId, Guid? ShopId, Guid? TeamLeaderId, string? Password);
    public sealed record UpdateUserRequest(string? FullName, string? Email, string? PhoneNumber, string? Position, string? Role, Guid? SectionId, Guid? HangarId, Guid? ShopId, Guid? ReportsToUserId, bool? IsActive);
    public sealed record CreateAssignmentRequest(Guid UserId, Guid WorkspaceId, string AssignmentType, DateTimeOffset StartAt, DateTimeOffset? EndAt);
    public sealed record UpdateAssignmentRequest(string? AssignmentType, DateTimeOffset? StartAt, DateTimeOffset? EndAt, bool? IsActive);
    private sealed record WorkspaceResolution(string WorkspaceType, Guid SectionId, Guid? HangarId, Guid? ShopId);
    private sealed record BulkImportUserRecord(string EmployeeId, string FullName, string? Email, string Position, string Role, string? SectionCode, string? HangarCode, string? ShopCode, string? Password);
    private class BulkImportResult
    {
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
