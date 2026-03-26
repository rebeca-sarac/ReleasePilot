using ErrorOr;
using MediatR;

namespace ReleasePilot.Application.Queries.GetPromotionById;

public record GetPromotionByIdQuery : IRequest<ErrorOr<PromotionDetailResponse>>
{
    public Guid PromotionId { get; init; }
}
