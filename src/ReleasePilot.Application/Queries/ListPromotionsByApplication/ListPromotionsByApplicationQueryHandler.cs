using ErrorOr;
using MediatR;
using ReleasePilot.Application.Common;
using ReleasePilot.Application.Ports;

namespace ReleasePilot.Application.Queries.ListPromotionsByApplication;

public class ListPromotionsByApplicationQueryHandler : IRequestHandler<ListPromotionsByApplicationQuery, ErrorOr<PagedResponse<PromotionSummaryResponse>>>
{
    private readonly IPromotionReadRepository _readRepository;

    public ListPromotionsByApplicationQueryHandler(IPromotionReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<ErrorOr<PagedResponse<PromotionSummaryResponse>>> Handle(ListPromotionsByApplicationQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
        {
            return Error.Validation(code: "Pagination.InvalidPage",
                                    description: "Page number must be 1 or greater.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            return Error.Validation(code: "Pagination.InvalidPageSize",
                                    description: "Page size must be between 1 and 100.");
        }

        var result = await _readRepository.ListByApplicationAsync(query.ApplicationId,
                                                                  query.Page,
                                                                  query.PageSize,
                                                                  cancellationToken);

        return result;
    }
}
