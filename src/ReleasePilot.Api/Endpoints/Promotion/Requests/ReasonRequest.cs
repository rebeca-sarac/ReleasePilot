namespace ReleasePilot.Api.Endpoints.Promotion.Requests;

public record ReasonRequest
{
    public required string Reason { get; init; }
}
