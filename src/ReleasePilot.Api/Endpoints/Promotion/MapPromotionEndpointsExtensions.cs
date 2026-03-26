using ReleasePilot.Application.Common;
using ReleasePilot.Application.Queries.GetPromotionById;
using ReleasePilot.Application.Queries.GetEnvironmentStatus;
using ReleasePilot.Application.Queries.ListPromotionsByApplication;

namespace ReleasePilot.Api.Endpoints.Promotion;

internal static class MapPromotionEndpointsExtensions
{
    internal static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Promotions.Base, PromotionEndpoints.RequestPromotion)
           .RequireAuthorization()
           .Produces(StatusCodes.Status201Created)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapPut(ApiEndpoints.Promotions.Approve, PromotionEndpoints.ApprovePromotion)
           .RequireAuthorization()
           .Produces(StatusCodes.Status204NoContent)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status404NotFound)
           .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapPut(ApiEndpoints.Promotions.Start, PromotionEndpoints.StartDeployment)
           .RequireAuthorization()
           .Produces(StatusCodes.Status204NoContent)
           .ProducesProblem(StatusCodes.Status404NotFound)
           .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapPut(ApiEndpoints.Promotions.Complete, PromotionEndpoints.CompletePromotion)
           .RequireAuthorization()
           .Produces(StatusCodes.Status204NoContent)
           .ProducesProblem(StatusCodes.Status404NotFound)
           .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapPut(ApiEndpoints.Promotions.Rollback, PromotionEndpoints.RollbackPromotion)
           .RequireAuthorization()
           .Produces(StatusCodes.Status204NoContent)
           .ProducesProblem(StatusCodes.Status404NotFound)
           .ProducesProblem(StatusCodes.Status409Conflict)
           .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapPut(ApiEndpoints.Promotions.Cancel, PromotionEndpoints.CancelPromotion)
           .RequireAuthorization()
           .Produces(StatusCodes.Status204NoContent)
           .ProducesProblem(StatusCodes.Status404NotFound)
           .ProducesProblem(StatusCodes.Status409Conflict)
           .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapGet(ApiEndpoints.Promotions.GetById, PromotionEndpoints.GetPromotionById)
           .RequireAuthorization()
           .Produces<PromotionDetailResponse>()
           .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet(ApiEndpoints.Promotions.EnvironmentStatus, PromotionEndpoints.GetEnvironmentStatus)
           .RequireAuthorization()
           .Produces<IReadOnlyList<EnvironmentStatusResponse>>()
           .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet(ApiEndpoints.Promotions.Base, PromotionEndpoints.ListPromotionsByApplication)
           .RequireAuthorization()
           .Produces<PagedResponse<PromotionSummaryResponse>>();

        return app;
    }
}
