using ErrorOr;
using MediatR;
using ReleasePilot.Application.Ports;

namespace ReleasePilot.Application.Commands.StartDeployment;

public class StartDeploymentCommandHandler : IRequestHandler<StartDeploymentCommand, ErrorOr<Unit>>
{
    private readonly IPromotionRepository _repository;
    private readonly IDeploymentPort _deploymentPort;
    private readonly IEventPublisher _eventPublisher;

    public StartDeploymentCommandHandler(IPromotionRepository repository, IDeploymentPort deploymentPort, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _deploymentPort = deploymentPort;
        _eventPublisher = eventPublisher;
    }

    public async Task<ErrorOr<Unit>> Handle(StartDeploymentCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(command.PromotionId, cancellationToken);
        if (promotion is null)
        {
            return Error.NotFound(code: "Promotion.NotFound",
                                  description: $"Promotion '{command.PromotionId}' was not found.");
        }

        var result = promotion.StartDeployment(command.StartedBy);
        if (result.IsError)
        {
            return result.Errors;
        }

        await _deploymentPort.TriggerDeploymentAsync(promotion.Id,
                                                     promotion.ApplicationId,
                                                     promotion.Version,
                                                     promotion.TargetEnvironment,
                                                     cancellationToken);

        await _repository.UpdateAsync(promotion, cancellationToken);

        foreach (var evt in promotion.DomainEvents)
        {
            await _eventPublisher.PublishAsync(evt, cancellationToken);
        }

        promotion.ClearDomainEvents();

        return Unit.Value;
    }
}
