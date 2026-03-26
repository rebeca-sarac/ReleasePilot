using ReleasePilot.Domain.Promotions;

namespace ReleasePilot.Application.Ports;

public interface INotificationPort
{
    Task SendPromotionNotificationAsync(
        Guid promotionId,
        Guid applicationId,
        PromotionState newState,
        IEnumerable<string> recipients,
        CancellationToken cancellationToken = default);
}
