using ErrorOr;
using MediatR;

namespace ReleasePilot.Application.Queries.GetEnvironmentStatus;

public record GetEnvironmentStatusQuery : IRequest<ErrorOr<IReadOnlyList<EnvironmentStatusResponse>>>
{
    public Guid ApplicationId { get; init; }
}
