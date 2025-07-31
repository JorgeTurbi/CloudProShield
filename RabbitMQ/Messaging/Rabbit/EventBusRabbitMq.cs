using System.Collections.Concurrent;
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

    // NUEVO: Cache de definiciones de suscripción para recrearlas después
    private readonly ConcurrentDictionary<string, SubscriptionDefinition> _subscriptionDefinitions =
        new();

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

            // CRÍTICO: Reestablecer suscripciones existentes Y crear consumidores
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

        // Limpiar conexión inmediatamente
        CleanupConnection();

        // CRÍTICO: Iniciar proceso de reconexión inmediato
        _logger.LogInformation("🔄 Iniciando proceso de reconexión automática...");
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000); // Esperar un poco antes de reintentar
            await TryReconnectWithRetries();
        });
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

        _logger.LogInformation(
            "🐛 DEBUG: Registrando suscripción - RoutingKey: '{RoutingKey}', EventType: '{EventType}', HandlerType: '{HandlerType}'",
            routingKey,
            typeof(TEvent).Name,
            typeof(THandler).Name
        );

        // NUEVO: Guardar definición de suscripción para poder recrearla
        _subscriptionDefinitions.TryAdd(
            routingKey,
            new SubscriptionDefinition
            {
                EventType = typeof(TEvent),
                HandlerType = typeof(THandler),
                RoutingKey = routingKey,
            }
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

        // CRÍTICO: Solo crear consumidor si es la primera suscripción para este routingKey
        if (handlers.Count == 1)
        {
            if (IsConnected)
            {
                _ = Task.Run(() => CreateConsumerAsync(routingKey));
            }
            else
            {
                _logger.LogInformation(
                    "📋 RabbitMQ no conectado - suscripción {RoutingKey} registrada para crear cuando se conecte",
                    routingKey
                );
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

    // CRÍTICO: Método mejorado para reestablecer suscripciones
    private async Task ReestablishSubscriptionsAsync()
    {
        if (_disposed || !IsConnected)
            return;

        var subscriptionsCount = _subscriptions.Count;
        var definitionsCount = _subscriptionDefinitions.Count;

        _logger.LogInformation(
            "🔄 Reestableciendo suscripciones después de reconexión - Handlers: {HandlersCount}, Definiciones: {DefinitionsCount}",
            subscriptionsCount,
            definitionsCount
        );

        // NUEVO: Crear consumidores para todas las suscripciones que tienen handlers
        foreach (var routingKey in _subscriptions.Keys)
        {
            if (_subscriptions.TryGetValue(routingKey, out var handlers) && handlers.Count > 0)
            {
                try
                {
                    await CreateConsumerAsync(routingKey);
                    _logger.LogInformation(
                        "✅ Consumidor reestablecido para {RoutingKey}",
                        routingKey
                    );
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
        }

        _logger.LogInformation("✅ Suscripciones reestablecidas - procesando mensajes en cola...");
    }

    private async Task TryReconnectWithRetries()
    {
        if (_disposed)
            return;

        var maxRetries = 20; // Intentar por 20 veces
        var retryDelay = TimeSpan.FromSeconds(3);
        var attempt = 0;

        while (!_disposed && !IsConnected && attempt < maxRetries)
        {
            attempt++;
            try
            {
                _logger.LogInformation(
                    "🔄 Intento de reconexión {Attempt}/{MaxRetries}...",
                    attempt,
                    maxRetries
                );

                var connected = await TryConnectAsync();
                if (connected)
                {
                    _logger.LogInformation("✅ Reconexión exitosa en intento {Attempt}", attempt);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "❌ Error en intento de reconexión {Attempt}", attempt);
            }

            if (!_disposed && attempt < maxRetries)
            {
                _logger.LogDebug(
                    "⏳ Esperando {Delay} segundos antes del siguiente intento...",
                    retryDelay.TotalSeconds
                );
                await Task.Delay(retryDelay);
            }
        }

        if (!IsConnected && !_disposed)
        {
            _logger.LogWarning(
                "❌ No se pudo reconectar después de {MaxRetries} intentos",
                maxRetries
            );
        }
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

    // NUEVO: Clase helper para guardar definiciones de suscripción
    private sealed class SubscriptionDefinition
    {
        public Type EventType { get; set; } = null!;
        public Type HandlerType { get; set; } = null!;
        public string RoutingKey { get; set; } = null!;
    }
}
