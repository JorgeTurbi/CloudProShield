using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Messaging.Rabbit;

public sealed class EventBusRabbitMq : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchange;
    private readonly ILogger<EventBusRabbitMq> _log;
    private readonly IServiceProvider _root;
    private readonly ConcurrentDictionary<string, List<Func<ReadOnlyMemory<byte>, Task>>> _routes =
        new();

    public EventBusRabbitMq(
        IConfiguration cfg,
        IServiceProvider root,
        ILogger<EventBusRabbitMq> log
    )
    {
        _log = log;
        _exchange = cfg["RabbitMQ:ExchangeName"] ?? "EventBusExchange";
        _root = root;

        var factory = new ConnectionFactory
        {
            HostName = cfg["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(cfg["RabbitMQ:Port"] ?? "5672"),
            UserName = cfg["RabbitMQ:UserName"] ?? "guest",
            Password = cfg["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true,
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_exchange, ExchangeType.Direct, durable: true);
    }

    /* ---------- PUBLIC API ---------- */

    public void Publish(string routingKey, object message)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var props = _channel.CreateBasicProperties();
        props.DeliveryMode = 2;

        _channel.BasicPublish(_exchange, routingKey, props, body);
        _log.LogInformation("Evento {RK} publicado ({Bytes} bytes)", routingKey, body.Length);
    }

    public void Subscribe<TEvent, THandler>(string routingKey)
        where THandler : IIntegrationEventHandler<TEvent>
    {
        // 1) Mapea routingKey → lista de delegados
        var list = _routes.GetOrAdd(routingKey, _ => new());
        list.Add(async body =>
        {
            await using var scope = _root.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<THandler>();

            var evt = JsonSerializer.Deserialize<TEvent>(body.Span)!;
            await handler.HandleAsync(evt, CancellationToken.None);
        });

        // 2) Declara cola y binding SOLO la primera vez
        if (list.Count > 1)
            return; // ya existía

        var queue = $"{typeof(TEvent).Name}.Queue"; // p.ej. CustomerCreatedEvent.Queue
        _channel.QueueDeclare(queue, true, false, false);
        _channel.QueueBind(queue, _exchange, routingKey);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            if (_routes.TryGetValue(ea.RoutingKey, out var handlers))
            {
                var tasks = handlers.Select(h => h(ea.Body));
                await Task.WhenAll(tasks);
            }
            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queue, autoAck: false, consumer);
        _log.LogInformation("Suscrito a {RK} en {Queue}", routingKey, queue);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
