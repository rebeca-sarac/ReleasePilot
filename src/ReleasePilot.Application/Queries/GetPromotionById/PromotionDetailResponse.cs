using ReleasePilot.Application.Common;
using ReleasePilot.Domain.Promotions;

namespace ReleasePilot.Application.Queries.GetPromotionById;

public record PromotionDetailResponse
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Version { get; init; } = string.Empty;
    public string TargetEnvironment { get; init; } = string.Empty;
    public PromotionState State { get; init; }
    public string RequestedBy { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
    public string? ApprovedBy { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? RollbackReason { get; init; }
    public string? CancellationReason { get; init; }
    public IReadOnlyList<PromotionStateHistoryEntry> StateHistory { get; init; } = [];
    public IReadOnlyList<string> IssueReferences { get; init; } = [];
    public IReadOnlyList<WorkItemResponse> WorkItems { get; init; } = [];
}

public record PromotionStateHistoryEntry
{
    public PromotionState State { get; init; }
    public DateTime OccurredOn { get; init; }
    public string? Actor { get; init; }
}
