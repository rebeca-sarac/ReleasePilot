namespace ReleasePilot.Api.Endpoints.Auth;

internal static class MapAuthEndpointsExtensions
{
    internal static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Auth.Token, AuthEndpoints.GetToken)
           .WithName("GetToken")
           .AllowAnonymous()
           .Produces(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
