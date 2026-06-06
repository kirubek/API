using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BaseOps.Application.Common;
using BaseOps.Application.DTOs;
using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BaseOps.Infrastructure.Authentication;

public sealed class AuthService(BaseOpsDbContext dbContext, IPasswordHasher passwordHasher, IUserScopeResolver scopeResolver, IAuditService auditService, IOptions<JwtOptions> options) : IAuthService
{
    private readonly JwtOptions jwtOptions = options.Value;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, string correlationId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.Include(x => x.Section).FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId, cancellationToken);
        if (user is null)
        {
            await auditService.WriteAsync(null, "AUTH_LOGIN_FAILED", "ApplicationUser", null, null, new { request.EmployeeId, reason = "User not found" }, true, ipAddress, correlationId, cancellationToken);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }
        
        if (!user.IsActive)
        {
            await auditService.WriteAsync(user.Id, "AUTH_LOGIN_FAILED", "ApplicationUser", user.Id.ToString(), null, new { request.EmployeeId, reason = "User inactive" }, true, ipAddress, correlationId, cancellationToken);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }
        
        var passwordValid = passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            await auditService.WriteAsync(user.Id, "AUTH_LOGIN_FAILED", "ApplicationUser", user.Id.ToString(), null, new { request.EmployeeId, reason = "Invalid password" }, true, ipAddress, correlationId, cancellationToken);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        dbContext.UserSessions.Add(new UserSession { UserId = user.Id, CorrelationId = correlationId, IpAddress = ipAddress, UserAgent = userAgent });
        var response = await IssueTokensAsync(user, ipAddress, cancellationToken);
        await auditService.WriteAsync(user.Id, "AUTH_LOGIN_SUCCEEDED", "ApplicationUser", user.Id.ToString(), null, null, false, ipAddress, correlationId, cancellationToken);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, string correlationId, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens.Include(x => x.User).ThenInclude(x => x!.Section).FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (refreshToken is null || refreshToken.User is null || !refreshToken.IsActive || !refreshToken.User.IsActive)
        {
            await auditService.WriteAsync(refreshToken?.UserId, "AUTH_REFRESH_FAILED", "RefreshToken", refreshToken?.Id.ToString(), null, null, true, ipAddress, correlationId, cancellationToken);
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var rawReplacement = GenerateSecureToken();
        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        refreshToken.ReplacedByTokenHash = HashToken(rawReplacement);
        var response = await IssueTokensAsync(refreshToken.User, ipAddress, cancellationToken, rawReplacement);
        await auditService.WriteAsync(refreshToken.UserId, "AUTH_REFRESH_ROTATED", "RefreshToken", refreshToken.Id.ToString(), null, null, false, ipAddress, correlationId, cancellationToken);
        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, string? ipAddress, string correlationId, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (refreshToken is not null && refreshToken.RevokedAt is null)
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            await auditService.WriteAsync(refreshToken.UserId, "AUTH_LOGOUT", "RefreshToken", refreshToken.Id.ToString(), null, null, false, ipAddress, correlationId, cancellationToken);
        }
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken cancellationToken, string? existingRawRefreshToken = null)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.AccessTokenMinutes);
        var scope = scopeResolver.Resolve(user);
        var claims = new List<Claim>
        {
            new("userId", user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("role", user.Role.ToString()),
            new("sectionId", user.SectionId?.ToString() ?? string.Empty),
            new("hangarId", user.HangarId?.ToString() ?? string.Empty),
            new("shopId", user.ShopId?.ToString() ?? string.Empty),
            new("hasProductionPlannerAccess", scope.HasProductionPlannerAccess.ToString().ToLowerInvariant()),
            new("canCreateAumsReports", scope.CanCreateAumsReports.ToString().ToLowerInvariant()),
            new("canCreateCarryOverReports", scope.CanCreateCarryOverReports.ToString().ToLowerInvariant()),
            new("canCreatePostMortemReports", scope.CanCreatePostMortemReports.ToString().ToLowerInvariant())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var token = new JwtSecurityToken(jwtOptions.Issuer, jwtOptions.Audience, claims, expires: expiresAt.UtcDateTime, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var rawRefreshToken = existingRawRefreshToken ?? GenerateSecureToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.RefreshTokenDays),
            IpAddress = ipAddress
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new AuthResponse(accessToken, rawRefreshToken, expiresAt, ToDto(user, scope));
    }

    private static AuthUserDto ToDto(ApplicationUser user, UserScope scope)
    {
        var capabilities = new List<string>();
        if (scope.HasProductionPlannerAccess) capabilities.Add("PRODUCTION_PLANNER_OPERATIONS");
        if (scope.CanCreateAumsReports) capabilities.Add("AUMS_CREATE");
        if (scope.CanCreateCarryOverReports) capabilities.Add("CARRY_OVER_CREATE");
        if (scope.CanCreatePostMortemReports) capabilities.Add("POST_MORTEM_CREATE");

        // Map backend role names to frontend role names
        var role = user.Role switch
        {
            Domain.Enums.UserRole.SafetyInspector => "SafaInspector",
            _ => user.Role.ToString()
        };
        return new AuthUserDto(user.Id, user.EmployeeId, user.FullName, user.Email, role, user.Position, user.SectionId, user.HangarId, user.ShopId, user.ReportsToUserId, user.IsActive, user.MustChangePassword, capabilities);
    }

    private static string GenerateSecureToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
