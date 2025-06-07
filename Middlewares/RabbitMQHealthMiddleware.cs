using RabbitMQ.Messaging.Health;

namespace CloudShield.Middlewares;

public class RabbitMQHealthMiddleware
{
  private readonly RequestDelegate _next;
  private readonly IRabbitMQHealthService _healthService;
  private readonly ILogger<RabbitMQHealthMiddleware> _logger;

  public RabbitMQHealthMiddleware(
      RequestDelegate next,
      IRabbitMQHealthService healthService,
      ILogger<RabbitMQHealthMiddleware> logger)
  {
    _next = next;
    _healthService = healthService;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    // Endpoint de health check para RabbitMQ
    if (context.Request.Path == "/health/rabbitmq")
    {
      var isHealthy = await _healthService.CheckHealthAsync();
      context.Response.StatusCode = isHealthy ? 200 : 503;
      await context.Response.WriteAsync(isHealthy ? "Healthy" : "Unhealthy");
      return;
    }

    await _next(context);
  }
}