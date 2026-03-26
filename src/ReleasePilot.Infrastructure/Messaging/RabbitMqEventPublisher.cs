using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Common;

namespace ReleasePilot.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private const string ExchangeName = "promotion.events";

    private readonly IConnectionFactory _factory;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    private IConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger)
    {
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

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var connection = await GetOrCreateConnectionAsync(cancellationToken);

        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(exchange: ExchangeName,
                                           type: ExchangeType.Fanout,
                                           durable: true,
                                           autoDelete: false,
                                           arguments: null,
                                           cancellationToken: cancellationToken);

        var envelope = new EventEnvelope
        {
            EventType = domainEvent.GetType().Name,
            EventId = domainEvent.EventId,
            OccurredOn = domainEvent.OccurredOn,
            Payload = JsonSerializer.SerializeToElement(domainEvent, domainEvent.GetType())
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(envelope);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = domainEvent.EventId.ToString()
        };

        await channel.BasicPublishAsync(exchange: ExchangeName,
                                        routingKey: string.Empty,
                                        mandatory: false,
                                        basicProperties: props,
                                        body: body,
                                        cancellationToken: cancellationToken);

        _logger.LogDebug("Published {EventType} (EventId={EventId}) to exchange '{Exchange}'", domainEvent.GetType().Name, domainEvent.EventId, ExchangeName);
    }

    private async Task<IConnection> GetOrCreateConnectionAsync(CancellationToken ct)
    {
        if (_connection?.IsOpen == true)
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(ct);

        try
        {
            if (_connection?.IsOpen != true)
                _connection = await ((ConnectionFactory)_factory).CreateConnectionAsync(ct);

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }

    public record EventEnvelope
    {
        public string EventType { get; init; } = string.Empty;
        public Guid EventId { get; init; }
        public DateTime OccurredOn { get; init; }
        public JsonElement Payload { get; init; }
    }
}
