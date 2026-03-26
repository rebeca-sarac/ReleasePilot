using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Application.Ports;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid promotionId, CancellationToken cancellationToken = default);

    Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);

    Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when an InProgress promotion already exists for the given
    /// application and target environment, indicating the slot is occupied.
    /// </summary>
    Task<bool> ExistsInProgressAsync(
        ApplicationId applicationId,
        EnvironmentName targetEnvironment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when at least one Completed promotion exists for the given
    /// application and environment, confirming the environment has been reached.
    /// </summary>
    Task<bool> HasCompletedPromotionAsync(
        ApplicationId applicationId,
        EnvironmentName environment,
        CancellationToken cancellationToken = default);
}
