using RabbitMQ.Client;

namespace RabbitMQ.Messaging.Health;

public sealed class RabbitMQHealthService : IRabbitMQHealthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQHealthService> _logger;
    private bool _lastHealthCheck = false;
    private DateTime _lastCheckTime = DateTime.MinValue;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public RabbitMQHealthService(IConfiguration configuration, ILogger<RabbitMQHealthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsHealthy => _lastHealthCheck;

    public async Task<bool> CheckHealthAsync()
    {
        // Cache del health check por 1 minuto
        if (DateTime.UtcNow - _lastCheckTime < _checkInterval)
        {
            return _lastHealthCheck;
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                RequestedHeartbeat = TimeSpan.FromSeconds(10)
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            
            // Test básico
            channel.ExchangeDeclare("health-check-test", ExchangeType.Direct, durable: false, autoDelete: true);
            
            _lastHealthCheck = true;
            _lastCheckTime = DateTime.UtcNow;
            
            _logger.LogDebug("RabbitMQ health check exitoso");
            return true;
        }
        catch (Exception ex)
        {
            _lastHealthCheck = false;
            _lastCheckTime = DateTime.UtcNow;
            
            _logger.LogWarning(ex, "RabbitMQ health check falló - continuando sin RabbitMQ");
            return false;
        }
    }
}