using ErrorOr;
using MediatR;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Application.Commands.RequestPromotion;

public class RequestPromotionCommandHandler : IRequestHandler<RequestPromotionCommand, ErrorOr<Guid>>
{
    private readonly IPromotionRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public RequestPromotionCommandHandler(IPromotionRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ErrorOr<Guid>> Handle(RequestPromotionCommand command, CancellationToken cancellationToken)
    {
        var appIdResult = ApplicationId.Create(command.ApplicationId);
        if (appIdResult.IsError)
        {
            return appIdResult.Errors;
        }

        var versionResult = ApplicationVersion.Create(command.Version);
        if (versionResult.IsError)
        {
            return versionResult.Errors;
        }

        var envResult = EnvironmentName.Create(command.TargetEnvironment);
        if (envResult.IsError)
        {
            return envResult.Errors;
        }

        EnvironmentName? sourceEnvironment = null;
        if (command.SourceEnvironment is not null)
        {
            var sourceEnvResult = EnvironmentName.Create(command.SourceEnvironment);
            if (sourceEnvResult.IsError)
            {
                return sourceEnvResult.Errors;
            }
            sourceEnvironment = sourceEnvResult.Value;
        }

        var predecessor = envResult.Value.PreviousEnvironment();
        if (predecessor is not null)
        {
            var predecessorCompleted = await _repository.HasCompletedPromotionAsync(appIdResult.Value, predecessor, cancellationToken);

            if (!predecessorCompleted)
            {
                return PromotionErrors.EnvironmentNotReady;
            }
        }

        var promotionResult = Promotion.Request(appIdResult.Value,
                                                versionResult.Value,
                                                envResult.Value,
                                                sourceEnvironment,
                                                command.RequestedBy,
                                                command.IssueReferences);

        if (promotionResult.IsError)
        {
            return promotionResult.Errors;
        }

        var promotion = promotionResult.Value;

        await _repository.AddAsync(promotion, cancellationToken);

        foreach (var evt in promotion.DomainEvents)
        {
            await _eventPublisher.PublishAsync(evt, cancellationToken);
        }

        promotion.ClearDomainEvents();

        return promotion.Id;
    }
}
