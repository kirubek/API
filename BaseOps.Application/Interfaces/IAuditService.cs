namespace BaseOps.Application.Interfaces;

public interface IAuditService
{
    Task WriteAsync(Guid? userId, string action, string entityName, string? entityId, object? beforeValues, object? afterValues, bool isAuthorizationFailure, string? ipAddress, string correlationId, CancellationToken cancellationToken);
}
