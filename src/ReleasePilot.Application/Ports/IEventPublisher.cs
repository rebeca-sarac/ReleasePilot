using ReleasePilot.Domain.Common;

namespace ReleasePilot.Application.Ports;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
