using ReleasePilot.Api.Auth;
using ReleasePilot.Api.Endpoints.Auth.Requests;

namespace ReleasePilot.Api.Endpoints.Auth;

internal static class AuthEndpoints
{
    internal static IResult GetToken(TokenRequest request, StubUserService users, TokenService tokens)
    {
        var user = users.FindByUsername(request.Username);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        var token = tokens.GenerateToken(user);
        return Results.Ok(new { token });
    }
}
