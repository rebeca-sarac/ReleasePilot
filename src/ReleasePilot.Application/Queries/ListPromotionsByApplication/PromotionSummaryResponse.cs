using ReleasePilot.Domain.Promotions;

namespace ReleasePilot.Application.Queries.ListPromotionsByApplication;

public record PromotionSummaryResponse
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Version { get; init; } = string.Empty;
    public string TargetEnvironment { get; init; } = string.Empty;
    public PromotionState State { get; init; }
    public string RequestedBy { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
}
