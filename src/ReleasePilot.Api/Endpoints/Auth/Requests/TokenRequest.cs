namespace ReleasePilot.Api.Endpoints.Auth.Requests;

public record TokenRequest
{
    public required string Username { get; init; }
}
