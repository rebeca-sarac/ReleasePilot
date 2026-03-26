using ErrorOr;
using MediatR;

namespace ReleasePilot.Application.Commands.RollbackPromotion;

public record RollbackPromotionCommand : IRequest<ErrorOr<Unit>>
{
    public Guid PromotionId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string RolledBackBy { get; init; } = string.Empty;
}
