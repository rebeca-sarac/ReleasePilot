namespace ReleasePilot.Infrastructure.Persistence.Entities;

/// <summary>
/// Infrastructure entity persisted by <see cref="Messaging.AuditLogConsumer"/>
/// when a domain event arrives from the RabbitMQ exchange.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; init; }
    public required string EventType { get; init; }
    public Guid PromotionId { get; init; }
    public DateTime Timestamp { get; init; }
    public string? ActingUser { get; init; }
}
