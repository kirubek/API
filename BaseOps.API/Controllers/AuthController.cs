using BaseOps.API.Middleware;
using BaseOps.Application.CQRS.Commands;
using BaseOps.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaseOps.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    [HttpPost("/api/v1/auth/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new LoginCommand(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier), cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [HttpPost("/api/v1/auth/refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new RefreshTokenCommand(request, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.TraceIdentifier), cancellationToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    [HttpPost("/api/v1/auth/logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new LogoutCommand(request, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.TraceIdentifier), cancellationToken);
        return NoContent();
    }
}
