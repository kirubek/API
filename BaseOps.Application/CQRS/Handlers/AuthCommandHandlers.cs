using BaseOps.Application.CQRS.Commands;
using BaseOps.Application.DTOs;
using BaseOps.Application.Interfaces;
using MediatR;

namespace BaseOps.Application.CQRS.Handlers;

public sealed class LoginCommandHandler(IAuthService authService) : IRequestHandler<LoginCommand, AuthResponse>
{
    public Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request.Request, request.IpAddress, request.UserAgent, request.CorrelationId, cancellationToken);
}

public sealed class RefreshTokenCommandHandler(IAuthService authService) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) =>
        authService.RefreshAsync(request.Request, request.IpAddress, request.CorrelationId, cancellationToken);
}

public sealed class LogoutCommandHandler(IAuthService authService) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        await authService.LogoutAsync(request.Request, request.IpAddress, request.CorrelationId, cancellationToken);
}
