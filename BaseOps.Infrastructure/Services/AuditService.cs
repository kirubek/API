using System.Text.Json;
using BaseOps.Application.Interfaces;
using BaseOps.Domain.Entities;
using BaseOps.Infrastructure.Data;

namespace BaseOps.Infrastructure.Services;

public sealed class AuditService(BaseOpsDbContext dbContext) : IAuditService
{
    public async Task WriteAsync(Guid? userId, string action, string entityName, string? entityId, object? beforeValues, object? afterValues, bool isAuthorizationFailure, string? ipAddress, string correlationId, CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            BeforeValues = beforeValues is null ? null : JsonSerializer.Serialize(beforeValues),
            AfterValues = afterValues is null ? null : JsonSerializer.Serialize(afterValues),
            IsAuthorizationFailure = isAuthorizationFailure
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
