using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Messaging.Health;

namespace RabbitMQ.Messaging.Rabbit;

public sealed class EventBusRabbitMq : IEventBus, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly string _exchange;
    private readonly ILogger<EventBusRabbitMq> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQHealthService _healthService;
    private readonly object _connectionLock = new();
    private readonly ConcurrentDictionary<
        string,
        List<Func<ReadOnlyMemory<byte>, Task>>
    > _subscriptions = new();

    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed = false;
    private bool _isReconnecting = false;

    public EventBusRabbitMq(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<EventBusRabbitMq> logger,
        IRabbitMQHealthService healthService
    )
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _healthService = healthService;
        _exchange = configuration["RabbitMQ:ExchangeName"] ?? "EventBusExchange";

        // Suscribirse a cambios de salud para reconexión automática
        if (_healthService is RabbitMQHealthService healthServiceConcrete)
        {
            healthServiceConcrete.OnHealthStatusChanged += OnHealthStatusChanged;
        }

        // Intentar conexión inicial (no bloqueante)
        _ = Task.Run(InitializeConnectionAsync);
    }

    public bool IsConnected => _connection?.IsOpen == true && _channel?.IsOpen == true;

    private async Task InitializeConnectionAsync()
    {
        try
        {
            await TryConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "⚠️ Conexión inicial a RabbitMQ falló - funcionando en modo degradado"
            );
        }
    }

    private async Task<bool> TryConnectAsync()
    {
        if (_disposed || _isReconnecting)
            return false;

        lock (_connectionLock)
        {
            if (IsConnected)
                return true;

            if (_isReconnecting)
                return false;

            _isReconnecting = true;
        }

        try
        {
            _logger.LogDebug("🔄 Intentando conectar a RabbitMQ...");

            // Cleanup de conexiones anteriores
            CleanupConnection();

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                RequestedHeartbeat = TimeSpan.FromSeconds(30),
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                DispatchConsumersAsync = true,
                ClientProvidedName = $"CloudProShield-{Environment.MachineName}",
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Configurar eventos de conexión
            _connection.ConnectionShutdown += OnConnectionShutdown;
            _connection.ConnectionBlocked += OnConnectionBlocked;
            _connection.ConnectionUnblocked += OnConnectionUnblocked;

            // Declarar exchange
            _channel.ExchangeDeclare(_exchange, ExchangeType.Direct, durable: true);

            _logger.LogInformation(
                "✅ Conectado a RabbitMQ en {Host}:{Port}",
                _configuration["RabbitMQ:HostName"],
                _configuration["RabbitMQ:Port"]
            );

            // Reestablecer suscripciones existentes
            await ReestablishSubscriptionsAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "❌ Error conectando a RabbitMQ");
            CleanupConnection();
            return false;
        }
        finally
        {
            lock (_connectionLock)
            {
                _isReconnecting = false;
            }
        }
    }

    private void OnHealthStatusChanged(bool isHealthy)
    {
        if (isHealthy && !IsConnected)
        {
            _logger.LogInformation(
                "🔄 RabbitMQ detectado como saludable - intentando reconectar..."
            );
            _ = Task.Run(TryConnectAsync);
        }
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs args)
    {
        if (_disposed)
            return;

        _logger.LogWarning("⚠️ RabbitMQ connection shutdown: {Reason}", args.ReplyText);

        // El health service manejará la reconexión automática
    }

    private void OnConnectionBlocked(
        object? sender,
        RabbitMQ.Client.Events.ConnectionBlockedEventArgs args
    )
    {
        _logger.LogWarning("⚠️ RabbitMQ connection blocked: {Reason}", args.Reason);
    }

    private void OnConnectionUnblocked(object? sender, EventArgs args)
    {
        _logger.LogInformation("✅ RabbitMQ connection unblocked");
    }

    public void Publish(string routingKey, object message)
    {
        if (_disposed)
        {
            _logger.LogWarning("❌ Intento de publicar en EventBus disposed");
            return;
        }

        if (!IsConnected)
        {
            _logger.LogWarning(
                "⚠️ RabbitMQ no disponible - evento {RoutingKey} no publicado",
                routingKey
            );
            return;
        }

        try
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = _channel!.CreateBasicProperties();
            properties.DeliveryMode = 2; // Mensajes persistentes
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(_exchange, routingKey, properties, body);
            _logger.LogDebug(
                "📤 Evento {RoutingKey} publicado ({Bytes} bytes)",
                routingKey,
                body.Length
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error publicando evento {RoutingKey}", routingKey);
        }
    }

    public void Subscribe<TEvent, THandler>(string routingKey)
        where THandler : IIntegrationEventHandler<TEvent>
    {
        if (_disposed)
        {
            _logger.LogWarning("❌ Intento de suscribir en EventBus disposed");
            return;
        }

        // 🐛 DEBUG: Logging para troubleshooting
        _logger.LogInformation(
            "🐛 DEBUG: Registrando suscripción - RoutingKey: '{RoutingKey}', EventType: '{EventType}', HandlerType: '{HandlerType}'",
            routingKey,
            typeof(TEvent).Name,
            typeof(THandler).Name
        );

        // CRÍTICO: Siempre registrar la suscripción primero
        var handlers = _subscriptions.GetOrAdd(
            routingKey,
            _ => new List<Func<ReadOnlyMemory<byte>, Task>>()
        );

        handlers.Add(async body =>
        {
            try
            {
                // 🐛 DEBUG: Logging para troubleshooting
                var jsonString = System.Text.Encoding.UTF8.GetString(body.Span);
                _logger.LogInformation(
                    "🐛 DEBUG: Procesando evento {RoutingKey}, JSON: {Json}",
                    routingKey,
                    jsonString
                );

                await using var scope = _serviceProvider.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();

                var eventObj = JsonSerializer.Deserialize<TEvent>(body.Span);
                if (eventObj != null)
                {
                    _logger.LogInformation(
                        "🐛 DEBUG: Evento deserializado exitosamente, llamando handler"
                    );
                    await handler.HandleAsync(eventObj, CancellationToken.None);
                }
                else
                {
                    _logger.LogWarning(
                        "🐛 DEBUG: La deserialización retornó null para {RoutingKey}",
                        routingKey
                    );
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(
                    jsonEx,
                    "🐛 DEBUG: Error de deserialización JSON para {RoutingKey}",
                    routingKey
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando evento {RoutingKey}", routingKey);
            }
        });

        // CAMBIO CRÍTICO: Siempre intentar crear el consumidor, incluso si no hay conexión
        if (handlers.Count == 1) // Solo crear consumidor si es la primera suscripción
        {
            if (IsConnected)
            {
                _ = Task.Run(() => CreateConsumerAsync(routingKey));
            }
            else
            {
                _logger.LogInformation(
                    "📋 RabbitMQ no conectado - suscripción {RoutingKey} registrada para cuando se conecte",
                    routingKey
                );

                // CRÍTICO: Reintento activo para crear consumidor
                _ = Task.Run(async () =>
                {
                    while (!_disposed && !IsConnected)
                    {
                        await Task.Delay(3000); // Verificar cada 3 segundos

                        if (IsConnected)
                        {
                            await CreateConsumerAsync(routingKey);
                            _logger.LogInformation(
                                "✅ Consumidor creado para {RoutingKey} después de reconexión",
                                routingKey
                            );
                            break;
                        }
                    }
                });
            }
        }

        _logger.LogInformation(
            "📥 Suscrito a evento {RoutingKey} (Handlers: {Count})",
            routingKey,
            handlers.Count
        );
    }

    private async Task CreateConsumerAsync(string routingKey)
    {
        if (_disposed)
        {
            _logger.LogWarning("🚫 No se puede crear consumidor - EventBus disposed");
            return;
        }

        if (!IsConnected)
        {
            _logger.LogWarning(
                "⚠️ No conectado - no se puede crear consumidor para {RoutingKey}",
                routingKey
            );
            return;
        }

        try
        {
            // CRÍTICO: Usar el mismo formato que antes para mantener compatibilidad
            var queueName = $"{routingKey}.Queue";

            _logger.LogInformation(
                "🔧 Creando consumidor para {RoutingKey} en cola {QueueName}",
                routingKey,
                queueName
            );

            // Declarar cola como durable para persistir mensajes
            _channel!.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _channel.QueueBind(queueName, _exchange, routingKey);

            _logger.LogInformation(
                "✅ Cola {QueueName} declarada y bound a exchange {Exchange}",
                queueName,
                _exchange
            );

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, eventArgs) =>
            {
                _logger.LogInformation("📨 Mensaje recibido en {RoutingKey}", eventArgs.RoutingKey);

                if (_subscriptions.TryGetValue(eventArgs.RoutingKey, out var handlers))
                {
                    try
                    {
                        _logger.LogInformation(
                            "🔄 Procesando mensaje con {HandlerCount} handlers",
                            handlers.Count
                        );

                        var tasks = handlers.Select(handler => handler(eventArgs.Body));
                        await Task.WhenAll(tasks);

                        _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                        _logger.LogInformation(
                            "✅ Evento {RoutingKey} procesado y acknowledged",
                            eventArgs.RoutingKey
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "❌ Error procesando evento {RoutingKey}",
                            eventArgs.RoutingKey
                        );
                        _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "⚠️ No hay handlers para {RoutingKey} - acknowledged",
                        eventArgs.RoutingKey
                    );
                    _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }
            };

            // CRÍTICO: Configurar BasicConsume
            var consumerTag = _channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation(
                "🎯 Consumidor ACTIVO para {RoutingKey} en cola {QueueName} (tag: {ConsumerTag})",
                routingKey,
                queueName,
                consumerTag
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error crítico creando consumidor para {RoutingKey}",
                routingKey
            );
            throw;
        }
    }

    private async Task ReestablishSubscriptionsAsync()
    {
        if (_disposed || !IsConnected)
            return;

        _logger.LogInformation(
            "🔄 Reestableciendo {Count} suscripciones después de reconexión...",
            _subscriptions.Count
        );

        foreach (var routingKey in _subscriptions.Keys)
        {
            try
            {
                await CreateConsumerAsync(routingKey);
                _logger.LogInformation("✅ Consumidor reestablecido para {RoutingKey}", routingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error reestableciendo suscripción {RoutingKey}",
                    routingKey
                );
            }
        }

        _logger.LogInformation("✅ Suscripciones reestablecidas - procesando mensajes en cola...");
    }

    private void CleanupConnection()
    {
        try
        {
            _channel?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing channel");
        }

        try
        {
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing connection");
        }

        _channel = null;
        _connection = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Desuscribirse del health service
        if (_healthService is RabbitMQHealthService healthServiceConcrete)
        {
            healthServiceConcrete.OnHealthStatusChanged -= OnHealthStatusChanged;
        }

        CleanupConnection();
    }
}
