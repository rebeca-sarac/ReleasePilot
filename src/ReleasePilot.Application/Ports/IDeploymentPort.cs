using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Application.Ports;

public interface IDeploymentPort
{
    Task TriggerDeploymentAsync(
        Guid promotionId,
        ApplicationId applicationId,
        ApplicationVersion version,
        EnvironmentName targetEnvironment,
        CancellationToken cancellationToken = default);
}
