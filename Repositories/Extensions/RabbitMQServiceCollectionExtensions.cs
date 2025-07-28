using RabbitMQ.Integration.Handlers;
using RabbitMQ.Messaging;
using RabbitMQ.Messaging.Health;
using RabbitMQ.Messaging.NoOp;
using RabbitMQ.Messaging.Rabbit;

namespace CloudShield.Repositories.Extensions;

public static class RabbitMQServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQEventBus(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var rabbitMQEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", true);

        if (rabbitMQEnabled)
        {
            Console.WriteLine("🐰 Configurando RabbitMQ...");

            // Registrar health service
            services.AddSingleton<IRabbitMQHealthService, RabbitMQHealthService>();

            // Registrar implementaciones de EventBus
            services.AddSingleton<EventBusRabbitMq>();
            services.AddSingleton<NoOpEventBus>();

            // Registrar handlers para eventos
            services.AddScoped<CustomerCreatedEventHandler>();
            services.AddScoped<AccountRegisteredEventHandler>();
            services.AddScoped<SecureDocumentAccessRequestedEventHandler>();
            services.AddScoped<DocumentReadyToSealEventHandler>();

            // Factory inteligente para determinar qué EventBus usar
            services.AddSingleton<IEventBus>(serviceProvider =>
            {
                var healthService = serviceProvider.GetRequiredService<IRabbitMQHealthService>();
                var logger = serviceProvider.GetRequiredService<ILogger<IEventBus>>();

                // Health check inicial NO BLOQUEANTE con timeout más corto
                var initialHealthTask = Task.Run(async () =>
                {
                    try
                    {
                        return await healthService.CheckHealthAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Error en health check inicial de RabbitMQ");
                        return false;
                    }
                });

                // CRÍTICO: Reducir timeout a 1 segundo para startup más rápido
                var isHealthy =
                    initialHealthTask.Wait(TimeSpan.FromSeconds(1)) && initialHealthTask.Result;

                if (isHealthy)
                {
                    logger.LogInformation("✅ RabbitMQ disponible - usando EventBusRabbitMq");
                    return serviceProvider.GetRequiredService<EventBusRabbitMq>();
                }
                else
                {
                    logger.LogWarning("⚠️ RabbitMQ no disponible al inicio - usando NoOpEventBus");
                    logger.LogInformation(
                        "🔄 Se reintentará conexión automáticamente en segundo plano"
                    );

                    // Programar verificación en segundo plano para posible upgrade a RabbitMQ
                    _ = Task.Run(async () =>
                    {
                        await MonitorRabbitMQAvailability(healthService, logger);
                    });

                    return serviceProvider.GetRequiredService<NoOpEventBus>();
                }
            });

            Console.WriteLine("✅ RabbitMQ configurado con reconexión automática");
        }
        else
        {
            Console.WriteLine("🔕 RabbitMQ deshabilitado en configuración - usando NoOpEventBus");
            services.AddSingleton<IEventBus, NoOpEventBus>();
        }

        return services;
    }

    public static WebApplication ConfigureRabbitMQSubscriptions(this WebApplication app)
    {
        // CRÍTICO: Configurar suscripciones INMEDIATAMENTE AL INICIAR
        // Esto debe ejecutarse ANTES de que cualquier mensaje llegue
        try
        {
            var bus = app.Services.GetRequiredService<IEventBus>();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            if (bus is EventBusRabbitMq rabbitmqBus)
            {
                logger.LogInformation("🔧 Configurando suscripciones RabbitMQ INMEDIATAMENTE...");

                // CRÍTICO: Configurar todas las suscripciones SIN DELAY
                bus.Subscribe<
                    RabbitMQ.Contracts.Events.CustomerCreatedEvent,
                    CustomerCreatedEventHandler
                >("CustomerCreatedEvent");
                bus.Subscribe<
                    RabbitMQ.Contracts.Events.AccountRegisteredEvent,
                    AccountRegisteredEventHandler
                >("AccountRegisteredEvent");
                bus.Subscribe<
                    RabbitMQ.Contracts.Events.SecureDocumentAccessRequestedEvent,
                    SecureDocumentAccessRequestedEventHandler
                >("SecureDocumentAccessRequestedEvent");
                bus.Subscribe<
                    RabbitMQ.Contracts.Events.DocumentReadyToSealEvent,
                    DocumentReadyToSealEventHandler
                >("DocumentReadyToSealEvent");

                logger.LogInformation("✅ Suscripciones RabbitMQ configuradas INMEDIATAMENTE");

                // SOLO después de suscribirse, verificar el procesamiento
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    logger.LogInformation("🔄 Verificando procesamiento de mensajes en cola...");
                });
            }
            else if (bus is NoOpEventBus)
            {
                logger.LogInformation("ℹ️ Usando NoOpEventBus - suscripciones omitidas");
            }
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "❌ CRÍTICO: Error configurando suscripciones RabbitMQ");
            throw; // Re-lanzar para que sea visible
        }

        return app;
    }

    private static async Task MonitorRabbitMQAvailability(
        IRabbitMQHealthService healthService,
        ILogger logger
    )
    {
        var checkInterval = TimeSpan.FromSeconds(30);
        var isHealthy = false;

        while (!isHealthy)
        {
            try
            {
                await Task.Delay(checkInterval);
                isHealthy = await healthService.CheckHealthAsync();

                if (isHealthy)
                {
                    logger.LogInformation(
                        "🎉 RabbitMQ ahora disponible! Para funcionalidad completa de messaging, considere reiniciar la aplicación"
                    );
                    break;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error monitoreando disponibilidad de RabbitMQ");
                // Continuar reintentando
            }
        }
    }
}
