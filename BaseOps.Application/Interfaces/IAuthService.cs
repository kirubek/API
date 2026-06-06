using BaseOps.Application.DTOs;

namespace BaseOps.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, string correlationId, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, string correlationId, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, string? ipAddress, string correlationId, CancellationToken cancellationToken);
}
