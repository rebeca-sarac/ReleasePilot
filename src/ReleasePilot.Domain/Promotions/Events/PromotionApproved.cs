using ReleasePilot.Domain.Common;

namespace ReleasePilot.Domain.Promotions.Events;

public record PromotionApproved : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid PromotionId { get; init; }
    public string ApprovedBy { get; init; } = null!;
}
