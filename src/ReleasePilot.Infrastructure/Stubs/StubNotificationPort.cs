using Microsoft.Extensions.Logging;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Promotions;

namespace ReleasePilot.Infrastructure.Stubs;

public class StubNotificationPort : INotificationPort
{
    private readonly ILogger<StubNotificationPort> _logger;

    public StubNotificationPort(ILogger<StubNotificationPort> logger)
    {
        _logger = logger;
    }

    public Task SendPromotionNotificationAsync(Guid promotionId, Guid applicationId, PromotionState newState,
                                               IEnumerable<string> recipients, CancellationToken cancellationToken = default)
    {
        var recipientList = string.Join(", ", recipients);

        _logger.LogInformation("[STUB] Notification sent — PromotionId={PromotionId}, App={AppId}, NewState={State}, Recipients=[{Recipients}]",
                                promotionId, applicationId, newState, recipientList);

        return Task.CompletedTask;
    }
}
