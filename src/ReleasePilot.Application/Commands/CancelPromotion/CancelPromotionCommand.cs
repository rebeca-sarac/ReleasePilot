using ErrorOr;
using MediatR;

namespace ReleasePilot.Application.Commands.CancelPromotion;

public record CancelPromotionCommand : IRequest<ErrorOr<Unit>>
{
    public Guid PromotionId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string CancelledBy { get; init; } = string.Empty;
}
