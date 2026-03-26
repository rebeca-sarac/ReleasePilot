using ErrorOr;
using MediatR;

namespace ReleasePilot.Application.Commands.CompletePromotion;

public record CompletePromotionCommand : IRequest<ErrorOr<Unit>>
{
    public Guid PromotionId { get; init; }
    public string CompletedBy { get; init; } = string.Empty;
}
