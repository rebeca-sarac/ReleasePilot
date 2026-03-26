using ErrorOr;
using MediatR;
using ReleasePilot.Application.Ports;

namespace ReleasePilot.Application.Queries.GetPromotionById;

public class GetPromotionByIdQueryHandler : IRequestHandler<GetPromotionByIdQuery, ErrorOr<PromotionDetailResponse>>
{
    private readonly IPromotionReadRepository _readRepository;
    private readonly IIssueTrackerPort _issueTracker;

    public GetPromotionByIdQueryHandler(IPromotionReadRepository readRepository, IIssueTrackerPort issueTracker)
    {
        _readRepository = readRepository;
        _issueTracker = issueTracker;
    }

    public async Task<ErrorOr<PromotionDetailResponse>> Handle(GetPromotionByIdQuery query, CancellationToken cancellationToken)
    {
        var promotion = await _readRepository.GetDetailByIdAsync(query.PromotionId, cancellationToken);

        if (promotion is null)
        {
            return Error.NotFound(code: "Promotion.NotFound",
                                  description: $"Promotion '{query.PromotionId}' was not found.");
        }

        if (promotion.IssueReferences.Count == 0)
        {
            return promotion;
        }

        var workItems = await _issueTracker.GetWorkItemsAsync(promotion.IssueReferences, cancellationToken);

        return promotion with { WorkItems = workItems };
    }
}
