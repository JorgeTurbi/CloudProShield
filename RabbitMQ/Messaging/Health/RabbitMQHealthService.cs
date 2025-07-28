using RabbitMQ.Client;

namespace RabbitMQ.Messaging.Health;

public sealed class RabbitMQHealthService : IRabbitMQHealthService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQHealthService> _logger;
    private readonly Timer _reconnectionTimer;
    private readonly object _lockObject = new();

    private bool _isHealthy = false;
    private DateTime _lastCheckTime = DateTime.MinValue;
    private bool _isChecking = false;
    private bool _disposed = false;

    // Cache del health check por 30 segundos (más frecuente que antes)
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public RabbitMQHealthService(
        IConfiguration configuration,
        ILogger<RabbitMQHealthService> logger
    )
    {
        _configuration = configuration;
        _logger = logger;

        // Timer para health checks automáticos cada 15 segundos
        _reconnectionTimer = new Timer(
            AutoHealthCheck,
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(15)
        );
    }

    public bool IsHealthy
    {
        get
        {
            lock (_lockObject)
            {
                return _isHealthy;
            }
        }
    }

    public async Task<bool> CheckHealthAsync()
    {
        lock (_lockObject)
        {
            // Cache del health check
            if (DateTime.UtcNow - _lastCheckTime < _checkInterval)
            {
                return _isHealthy;
            }

            // Evitar múltiples checks simultáneos
            if (_isChecking)
            {
                return _isHealthy;
            }

            _isChecking = true;
        }

        try
        {
            var result = await PerformHealthCheckAsync();

            lock (_lockObject)
            {
                _isHealthy = result;
                _lastCheckTime = DateTime.UtcNow;
                _isChecking = false;
            }

            if (result)
            {
                _logger.LogDebug("✅ RabbitMQ health check exitoso");
            }
            else
            {
                _logger.LogDebug("❌ RabbitMQ health check falló");
            }

            return result;
        }
        catch (Exception ex)
        {
            lock (_lockObject)
            {
                _isHealthy = false;
                _lastCheckTime = DateTime.UtcNow;
                _isChecking = false;
            }

            _logger.LogDebug(ex, "❌ RabbitMQ health check excepción");
            return false;
        }
    }

    private async Task<bool> PerformHealthCheckAsync()
    {
        if (_disposed)
            return false;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3), // Timeout más corto
                RequestedHeartbeat = TimeSpan.FromSeconds(10),
            };

            // Usar using para asegurar limpieza de recursos
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Test básico - declarar exchange temporal
            channel.ExchangeDeclare(
                "health-check-test",
                ExchangeType.Direct,
                durable: false,
                autoDelete: true
            );

            return true;
        }
        catch (Exception ex)
            when (ex is RabbitMQ.Client.Exceptions.BrokerUnreachableException
                || ex is System.Net.Sockets.SocketException
                || ex is RabbitMQ.Client.Exceptions.ConnectFailureException
                || ex is TimeoutException
            )
        {
            // Estos son errores esperados cuando RabbitMQ no está disponible
            return false;
        }
        catch (Exception ex)
        {
            // Otros errores inesperados
            _logger.LogWarning(ex, "Error inesperado en RabbitMQ health check");
            return false;
        }
    }

    private void AutoHealthCheck(object? state)
    {
        if (_disposed)
            return;

        // Health check automático en segundo plano
        _ = Task.Run(async () =>
        {
            try
            {
                var wasHealthy = IsHealthy;
                var isNowHealthy = await CheckHealthAsync();

                // Log solo cambios de estado
                if (wasHealthy != isNowHealthy)
                {
                    if (isNowHealthy)
                    {
                        _logger.LogInformation("🔄 RabbitMQ reconectado - servicio disponible");
                        OnHealthStatusChanged?.Invoke(true);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ RabbitMQ desconectado - usando modo degradado");
                        OnHealthStatusChanged?.Invoke(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en auto health check de RabbitMQ");
            }
        });
    }

    /// <summary>
    /// Evento que se dispara cuando cambia el estado de salud de RabbitMQ
    /// </summary>
    public event Action<bool>? OnHealthStatusChanged;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _reconnectionTimer?.Dispose();
    }
}
