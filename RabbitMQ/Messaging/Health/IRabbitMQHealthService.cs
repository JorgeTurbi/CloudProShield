namespace RabbitMQ.Messaging.Health;

public interface IRabbitMQHealthService
{
    bool IsHealthy { get; }
    Task<bool> CheckHealthAsync();
}
