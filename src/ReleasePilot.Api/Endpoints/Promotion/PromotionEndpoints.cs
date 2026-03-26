using System.Security.Claims;
using MediatR;
using ReleasePilot.Api.Endpoints.Promotion.Requests;
using ReleasePilot.Api.Errors;
using ReleasePilot.Application.Commands.ApprovePromotion;
using ReleasePilot.Application.Commands.CancelPromotion;
using ReleasePilot.Application.Commands.CompletePromotion;
using ReleasePilot.Application.Commands.RequestPromotion;
using ReleasePilot.Application.Commands.RollbackPromotion;
using ReleasePilot.Application.Commands.StartDeployment;
using ReleasePilot.Application.Queries.GetEnvironmentStatus;
using ReleasePilot.Application.Queries.GetPromotionById;
using ReleasePilot.Application.Queries.ListPromotionsByApplication;

namespace ReleasePilot.Api.Endpoints.Promotion;

internal static class PromotionEndpoints
{
    internal static async Task<IResult> RequestPromotion(RequestPromotionRequest request, ClaimsPrincipal user, ISender bus, CancellationToken cancellationToken)
    {
        var requestedBy = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var command = new RequestPromotionCommand
        {
            ApplicationId     = request.ApplicationId,
            Version           = request.Version,
            TargetEnvironment = request.TargetEnvironment,
            SourceEnvironment = request.SourceEnvironment,
            RequestedBy       = requestedBy,
            IssueReferences   = request.IssueReferences
        };

        var result = await bus.Send(command, cancellationToken);

        if (result.IsError)
        {
            return result.ToProblemResult();
        }

        return Results.Created($"/api/promotions/{result.Value}", new { id = result.Value });
    }

    internal static async Task<IResult> ApprovePromotion(Guid id, ClaimsPrincipal user, ISender bus, CancellationToken cancellationToken)
    {
        var approvedBy = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var approverRole = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        var command = new ApprovePromotionCommand
        {
            PromotionId = id,
            ApprovedBy = approvedBy,
            ApproverRole = approverRole
        };

        var result = await bus.Send(command, cancellationToken);

        if (result.IsError)
        {
            return result.ToProblemResult();
        }

        return Results.NoContent();
    }

    internal static async Task<IResult> StartDeployment(Guid id, ClaimsPrincipal user, ISender bus, CancellationToken cancellationToken)
    {
        var startedBy = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await bus.Send(new StartDeploymentCommand { PromotionId = id, StartedBy = startedBy }, cancellationToken);

        if (result.IsError)
        {
            return result.ToProblemResult();
        }

        return Results.NoContent();
    }

    internal static async Task<IResult> CompletePromotion(Guid id, ClaimsPrincipal user, ISender bus, CancellationToken cancellationToken)
    {
        var completedBy = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await bus.Send(new CompletePromotionCommand { PromotionId = id, CompletedBy = completedBy }, cancellationToken);

        if (result.IsError)
        {
            return result.ToProblemResult();
        }

        return Results.NoContent();
    }

    internal static async Task<IResult> RollbackPromotion(Guid id, ReasonRequest request, ClaimsPrincipal user, ISender bus, CancellationToken cancellationToken)
    {
        var rolledBackBy = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await bus.Send(new RollbackPromotionCommand { PromotionId = id, Reason = request.Reason, RolledBackBy = rolledBackBy }, cancellationToken);

        if (result.IsError)
        {
            return result.ToProblemResult();
        }

        return Results.NoContent();
    }

    internal static async Task<IResult> CancelPromotion(Guid id, ReasonRequest request, ClaimsPrincipal user, ISender bus, CancellationToken cancellationToken)
    {
        var cancelledBy = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await bus.Send(new CancelPromotionCommand { PromotionId = id, Reason = request.Reason, CancelledBy = cancelledBy }, cancellationToken);

        if (result.IsError)
        {
            return result.ToProblemResult();
        }

        return Results.NoContent();
    }

    internal static async Task<IResult> GetPromotionById(Guid id, ISender bus, CancellationToken cancellationToken)
    {
        var result = await bus.Send(new GetPromotionByIdQuery { PromotionId = id }, cancellationToken);

        if (result.IsError)
        {
            return result.ToProblemResult();
        }

        return Results.Ok(result.Value);
    }

    internal static async Task<IResult> GetEnvironmentStatus(Guid id, ISender bus, CancellationToken cancellationToken)
    {
        var result = await bus.Send(new GetEnvironmentStatusQuery { ApplicationId = id }, cancellationToken);

        if (result.IsError)
        {
            return result.ToProblemResult();
        }

        return Results.Ok(result.Value);
    }

    internal static async Task<IResult> ListPromotionsByApplication(Guid applicationId, ISender bus, CancellationToken cancellationToken, int page = 1, int pageSize = 20)
    {
        var query = new ListPromotionsByApplicationQuery
        {
            ApplicationId = applicationId,
            Page          = page,
            PageSize      = pageSize
        };
        
        var result = await bus.Send(query, cancellationToken);

        if (result.IsError)
        {
            return result.ToProblemResult();
        }

        return Results.Ok(result.Value);
    }
}
