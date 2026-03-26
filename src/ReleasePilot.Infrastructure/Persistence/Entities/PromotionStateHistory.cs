using ReleasePilot.Domain.Promotions;

namespace ReleasePilot.Infrastructure.Persistence.Entities;

public class PromotionStateHistory
{
    public Guid Id { get; init; }
    public Guid PromotionId { get; init; }
    public PromotionState State { get; init; }
    public DateTime OccurredOn { get; init; }
    public string? Actor { get; init; }
}
