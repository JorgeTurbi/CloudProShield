namespace RabbitMQ.Messaging.Health;

public interface IRabbitMQHealthService
{
    /// <summary>
    /// Estado actual de salud de RabbitMQ (cached)
    /// </summary>
    bool IsHealthy { get; }

    /// <summary>
    /// Realiza un health check de RabbitMQ
    /// </summary>
    /// <returns>True si RabbitMQ está disponible y funcionando</returns>
    Task<bool> CheckHealthAsync();
}
