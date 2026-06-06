namespace BaseOps.Application.DTOs;

public sealed record LoginRequest(string EmployeeId, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record AuthUserDto(Guid Id, string EmployeeId, string FullName, string? Email, string Role, string? Position, Guid? SectionId, Guid? HangarId, Guid? ShopId, Guid? ReportsToUserId, bool IsActive, bool MustChangePassword, IReadOnlyCollection<string> Capabilities);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, AuthUserDto User);
