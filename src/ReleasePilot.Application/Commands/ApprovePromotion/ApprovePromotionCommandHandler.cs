using ErrorOr;
using MediatR;
using ReleasePilot.Application.Ports;

namespace ReleasePilot.Application.Commands.ApprovePromotion;

public class ApprovePromotionCommandHandler : IRequestHandler<ApprovePromotionCommand, ErrorOr<Unit>>
{
    private readonly IPromotionRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public ApprovePromotionCommandHandler(IPromotionRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ErrorOr<Unit>> Handle(ApprovePromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(command.PromotionId, cancellationToken);
        if (promotion is null)
        {
            return Error.NotFound(code: "Promotion.NotFound",
                                  description: $"Promotion '{command.PromotionId}' was not found.");
        }

        // Guard: no existing InProgress promotion for the same app + environment
        var slotOccupied = await _repository.ExistsInProgressAsync(promotion.ApplicationId,
                                                                   promotion.TargetEnvironment,
                                                                   cancellationToken);

        if (slotOccupied)
        {
            return Error.Conflict(code: "Promotion.SlotOccupied",
                                  description: "An InProgress promotion already exists for this application and environment.");
        }

        var result = promotion.Approve(command.ApprovedBy, command.ApproverRole);
        if (result.IsError)
        {
            return result.Errors;
        }

        await _repository.UpdateAsync(promotion, cancellationToken);

        foreach (var evt in promotion.DomainEvents)
        {
            await _eventPublisher.PublishAsync(evt, cancellationToken);
        }

        promotion.ClearDomainEvents();

        return Unit.Value;
    }
}
