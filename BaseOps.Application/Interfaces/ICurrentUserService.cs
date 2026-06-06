namespace BaseOps.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? CorrelationId { get; }
    string? IpAddress { get; }
}
