using ReleasePilot.Api.Endpoints.Auth;
using ReleasePilot.Api.Endpoints.Promotion;

namespace ReleasePilot.Api.Endpoints;

public static class EndpointsExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPromotionEndpoints();
        app.MapAuthEndpoints();
        return app;
    }
}
