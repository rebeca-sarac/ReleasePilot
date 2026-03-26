using ReleasePilot.Domain.Common;
using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Domain.Promotions.Events;

public record PromotionRequested : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid PromotionId { get; init; }
    public string RequestedBy { get; init; } = null!;
    public ApplicationId ApplicationId { get; init; } = null!;
    public ApplicationVersion Version { get; init; } = null!;
    public EnvironmentName TargetEnvironment { get; init; } = null!;
}
