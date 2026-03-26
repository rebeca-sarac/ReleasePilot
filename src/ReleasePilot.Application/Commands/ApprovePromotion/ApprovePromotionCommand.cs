using ErrorOr;
using MediatR;

namespace ReleasePilot.Application.Commands.ApprovePromotion;

public record ApprovePromotionCommand : IRequest<ErrorOr<Unit>>
{
    public Guid PromotionId { get; init; }
    public string ApprovedBy { get; init; } = string.Empty;
    public string ApproverRole { get; init; } = string.Empty;
}
