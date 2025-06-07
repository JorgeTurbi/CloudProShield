using RabbitMQ.Messaging.Health;
using RabbitMQ.Messaging.NoOp;
using RabbitMQ.Messaging.Rabbit;

namespace RabbitMQ.Messaging.Factory;

public sealed class EventBusFactory : IEventBusFactory
{
  private readonly IServiceProvider _serviceProvider;
  private readonly IRabbitMQHealthService _healthService;
  private readonly ILogger<EventBusFactory> _logger;

  public EventBusFactory(
      IServiceProvider serviceProvider,
      IRabbitMQHealthService healthService,
      ILogger<EventBusFactory> logger)
  {
    _serviceProvider = serviceProvider;
    _healthService = healthService;
    _logger = logger;
  }

  public IEventBus CreateEventBus()
  {
    try
    {
      if (_healthService.CheckHealthAsync().GetAwaiter().GetResult())
      {
        _logger.LogInformation("Usando EventBusRabbitMq - RabbitMQ disponible");
        return _serviceProvider.GetRequiredService<EventBusRabbitMq>();
      }
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Error creando EventBusRabbitMq, usando NoOpEventBus");
    }

    _logger.LogWarning("Usando NoOpEventBus - RabbitMQ no disponible");
    return _serviceProvider.GetRequiredService<NoOpEventBus>();
  }
}