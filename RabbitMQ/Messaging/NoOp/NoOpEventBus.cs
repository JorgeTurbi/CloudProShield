using Microsoft.Extensions.Logging;

namespace RabbitMQ.Messaging.NoOp;

public sealed class NoOpEventBus : IEventBus
{
    private readonly ILogger<NoOpEventBus> _logger;

    public NoOpEventBus(ILogger<NoOpEventBus> logger)
    {
        _logger = logger;
    }

    public void Publish(string routingKey, object message)
    {
        _logger.LogWarning(
            "RabbitMQ no disponible - Evento {RoutingKey} no publicado: {Message}",
            routingKey,
            message.GetType().Name
        );
    }

    public void Subscribe<TEvent, THandler>(string routingKey)
        where THandler : IIntegrationEventHandler<TEvent>
    {
        _logger.LogWarning(
            "RabbitMQ no disponible - Suscripción a {RoutingKey} ignorada",
            routingKey
        );
    }
}
