using ReleasePilot.Application.Common;
using ReleasePilot.Application.Queries.GetEnvironmentStatus;
using ReleasePilot.Application.Queries.GetPromotionById;
using ReleasePilot.Application.Queries.ListPromotionsByApplication;

namespace ReleasePilot.Application.Ports;

/// <summary>
/// Read-side projection store. Implementations may use a denormalised
/// read model, views, or the same EF DbContext — the Application layer
/// does not care which.
/// </summary>
public interface IPromotionReadRepository
{
    Task<PromotionDetailResponse?> GetDetailByIdAsync(
        Guid promotionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvironmentStatusResponse>> GetEnvironmentStatusAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<PromotionSummaryResponse>> ListByApplicationAsync(
        Guid applicationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
