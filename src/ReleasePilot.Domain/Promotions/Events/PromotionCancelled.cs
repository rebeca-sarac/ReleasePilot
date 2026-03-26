using ReleasePilot.Domain.Common;

namespace ReleasePilot.Domain.Promotions.Events;

public record PromotionCancelled : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid PromotionId { get; init; }
    public string CancelledBy { get; init; } = null!;
    public string Reason { get; init; } = string.Empty;
}
