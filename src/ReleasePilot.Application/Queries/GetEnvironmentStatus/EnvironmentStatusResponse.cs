using ReleasePilot.Domain.Promotions;

namespace ReleasePilot.Application.Queries.GetEnvironmentStatus;

public record EnvironmentStatusResponse
{
    public string Environment { get; init; } = string.Empty;
    public PromotionState? ActiveState { get; init; }
    public Guid? ActivePromotionId { get; init; }
    public string? ActiveVersion { get; init; }
    public DateTime? LastActivityAt { get; init; }
}
