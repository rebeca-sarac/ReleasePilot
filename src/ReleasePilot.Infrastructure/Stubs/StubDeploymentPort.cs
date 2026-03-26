using Microsoft.Extensions.Logging;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Infrastructure.Stubs;

public class StubDeploymentPort : IDeploymentPort
{
    private readonly ILogger<StubDeploymentPort> _logger;

    public StubDeploymentPort(ILogger<StubDeploymentPort> logger)
    {
        _logger = logger;
    }

    public Task TriggerDeploymentAsync(Guid promotionId, ApplicationId applicationId, ApplicationVersion version,
                                       EnvironmentName targetEnvironment, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[STUB] Deployment triggered — PromotionId={PromotionId}, App={AppId}, " + "Version={Version}, Environment={Environment}",
                                promotionId, applicationId.Value, version.Value, targetEnvironment.Value);

        return Task.CompletedTask;
    }
}
