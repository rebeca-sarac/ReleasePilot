using ReleasePilot.Domain.Common;

namespace ReleasePilot.Domain.Promotions.Events;

public record PromotionCompleted : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid PromotionId { get; init; }
    public string CompletedBy { get; init; } = null!;
    public DateTime CompletedAt { get; init; }
}
