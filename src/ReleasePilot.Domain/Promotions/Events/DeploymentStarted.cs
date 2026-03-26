using ReleasePilot.Domain.Common;

namespace ReleasePilot.Domain.Promotions.Events;

public record DeploymentStarted : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid PromotionId { get; init; }
    public string StartedBy { get; init; } = null!;
}
