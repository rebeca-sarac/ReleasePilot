namespace ReleasePilot.Api.Endpoints.Promotion.Requests;

public record RequestPromotionRequest
{
    public required Guid ApplicationId { get; init; }
    public required string Version { get; init; }
    public required string TargetEnvironment { get; init; }
    public string? SourceEnvironment { get; init; }
    public IReadOnlyList<string>? IssueReferences { get; init; }
}
