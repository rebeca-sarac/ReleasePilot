using ErrorOr;
using MediatR;
using ReleasePilot.Application.Ports;

namespace ReleasePilot.Application.Commands.CancelPromotion;

public class CancelPromotionCommandHandler : IRequestHandler<CancelPromotionCommand, ErrorOr<Unit>>
{
    private readonly IPromotionRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly INotificationPort _notificationPort;

    public CancelPromotionCommandHandler(IPromotionRepository repository, IEventPublisher eventPublisher, INotificationPort notificationPort)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _notificationPort = notificationPort;
    }

    public async Task<ErrorOr<Unit>> Handle(CancelPromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(command.PromotionId, cancellationToken);
        if (promotion is null)
        {
            return Error.NotFound(code: "Promotion.NotFound",
                                  description: $"Promotion '{command.PromotionId}' was not found.");
        }

        var result = promotion.Cancel(command.Reason, command.CancelledBy);
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

        await _notificationPort.SendPromotionNotificationAsync(promotion.Id,
                                                               promotion.ApplicationId.Value,
                                                               promotion.State,
                                                               [promotion.RequestedBy],
                                                               cancellationToken);

        return Unit.Value;
    }
}
