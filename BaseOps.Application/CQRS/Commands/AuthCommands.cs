using BaseOps.Application.DTOs;
using MediatR;

namespace BaseOps.Application.CQRS.Commands;

public sealed record LoginCommand(LoginRequest Request, string? IpAddress, string? UserAgent, string CorrelationId) : IRequest<AuthResponse>;
public sealed record RefreshTokenCommand(RefreshTokenRequest Request, string? IpAddress, string CorrelationId) : IRequest<AuthResponse>;
public sealed record LogoutCommand(LogoutRequest Request, string? IpAddress, string CorrelationId) : IRequest;
