using ErrorOr;
using MediatR;
using ReleasePilot.Application.Ports;

namespace ReleasePilot.Application.Queries.GetEnvironmentStatus;

public class GetEnvironmentStatusQueryHandler: IRequestHandler<GetEnvironmentStatusQuery, ErrorOr<IReadOnlyList<EnvironmentStatusResponse>>>
{
    private readonly IPromotionReadRepository _readRepository;

    public GetEnvironmentStatusQueryHandler(IPromotionReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<ErrorOr<IReadOnlyList<EnvironmentStatusResponse>>> Handle(GetEnvironmentStatusQuery query, CancellationToken cancellationToken)
    {
        var statuses = await _readRepository.GetEnvironmentStatusAsync(query.ApplicationId, cancellationToken);

        return ErrorOrFactory.From(statuses);
    }
}
