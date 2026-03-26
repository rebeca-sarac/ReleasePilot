using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReleasePilot.Domain.Promotions;
using ReleasePilot.Infrastructure.Persistence;
using ReleasePilot.Infrastructure.Persistence.Entities;

namespace ReleasePilot.Infrastructure.Messaging;

public class AuditLogConsumer : BackgroundService
{
    private const string ExchangeName = "promotion.events";
    private const string QueueName    = "audit.promotion.events";

    private readonly ConnectionFactory _factory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLogConsumer> _logger;

    public AuditLogConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<AuditLogConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:Host"] ?? "localhost",
            Port = int.TryParse(configuration["RabbitMq:Port"], out var port) ? port : 5672,
            UserName = configuration["RabbitMq:Username"] ?? "guest",
            Password = configuration["RabbitMq:Password"] ?? "guest",
            VirtualHost = configuration["RabbitMq:VirtualHost"] ?? "/"
        };
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AuditLogConsumer starting.");

        await using var connection = await _factory.CreateConnectionAsync(cancellationToken);
        await using var channel    = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(exchange: ExchangeName,
                                           type: ExchangeType.Fanout,
                                           durable: true,
                                           autoDelete: false,
                                           cancellationToken: cancellationToken);

        var queueOk = await channel.QueueDeclareAsync(queue: QueueName,
                                                      durable: true,
                                                      exclusive: false,
                                                      autoDelete: false,
                                                      cancellationToken: cancellationToken);

        await channel.QueueBindAsync(queue: queueOk.QueueName,
                                     exchange: ExchangeName,
                                     routingKey: string.Empty,
                                     cancellationToken: cancellationToken);

        await channel.BasicQosAsync(prefetchSize: 0,
                                    prefetchCount: 1,
                                    global: false,
                                    cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();

            try
            {
                await ProcessMessageAsync(body, cancellationToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process audit message (DeliveryTag={Tag}).", ea.DeliveryTag);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken);
            }
        };

        await channel.BasicConsumeAsync(queue: queueOk.QueueName,
                                        autoAck: false,
                                        consumer: consumer,
                                        cancellationToken: cancellationToken);

        _logger.LogInformation("AuditLogConsumer listening on queue '{Queue}'.", QueueName);

        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        _logger.LogInformation("AuditLogConsumer stopping.");
    }

    private async Task ProcessMessageAsync(byte[] body, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(body));
        var root = doc.RootElement;

        var eventType   = root.GetProperty("EventType").GetString() ?? "Unknown";
        var eventId     = root.GetProperty("EventId").GetGuid();
        var occurredOn  = root.GetProperty("OccurredOn").GetDateTime();
        var payload     = root.GetProperty("Payload");

        Guid promotionId  = default;
        string? actingUser = null;

        if (payload.TryGetProperty("PromotionId", out var pidEl))
            promotionId = pidEl.GetGuid();

        actingUser = eventType switch
        {
            "PromotionRequested"  => payload.TryGetProperty("RequestedBy",  out var e1) ? e1.GetString() : null,
            "PromotionApproved"   => payload.TryGetProperty("ApprovedBy",   out var e2) ? e2.GetString() : null,
            "DeploymentStarted"   => payload.TryGetProperty("StartedBy",    out var e3) ? e3.GetString() : null,
            "PromotionCompleted"  => payload.TryGetProperty("CompletedBy",  out var e4) ? e4.GetString() : null,
            "PromotionRolledBack" => payload.TryGetProperty("RolledBackBy", out var e5) ? e5.GetString() : null,
            "PromotionCancelled"  => payload.TryGetProperty("CancelledBy",  out var e6) ? e6.GetString() : null,
            _                     => null
        };

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AuditLogs.Add(new AuditLog
        {
            Id = eventId,
            EventType = eventType,
            PromotionId = promotionId,
            Timestamp = occurredOn,
            ActingUser = actingUser
        });

        var stateForEvent = eventType switch
        {
            "PromotionRequested"  => (PromotionState?)PromotionState.Requested,
            "PromotionApproved"   => PromotionState.Approved,
            "DeploymentStarted"   => PromotionState.InProgress,
            "PromotionCompleted"  => PromotionState.Completed,
            "PromotionCancelled"  => PromotionState.Cancelled,
            "PromotionRolledBack" => PromotionState.RolledBack,
            _                     => (PromotionState?)null
        };

        if (stateForEvent.HasValue)
        {
            db.PromotionStateHistories.Add(new PromotionStateHistory
            {
                Id = Guid.NewGuid(),
                PromotionId = promotionId,
                State = stateForEvent.Value,
                OccurredOn = occurredOn,
                Actor = actingUser
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit log written: {EventType} for promotion {PromotionId}.", eventType, promotionId);
    }
}
