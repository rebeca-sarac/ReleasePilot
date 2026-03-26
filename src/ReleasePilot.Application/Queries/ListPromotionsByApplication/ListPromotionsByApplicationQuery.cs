using ErrorOr;
using MediatR;
using ReleasePilot.Application.Common;

namespace ReleasePilot.Application.Queries.ListPromotionsByApplication;

public record ListPromotionsByApplicationQuery : IRequest<ErrorOr<PagedResponse<PromotionSummaryResponse>>>
{
    public Guid ApplicationId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
