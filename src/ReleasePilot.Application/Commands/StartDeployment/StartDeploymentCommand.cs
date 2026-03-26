using ErrorOr;
using MediatR;

namespace ReleasePilot.Application.Commands.StartDeployment;

public record StartDeploymentCommand : IRequest<ErrorOr<Unit>>
{
    public Guid PromotionId { get; init; }
    public string StartedBy { get; init; } = string.Empty;
}
