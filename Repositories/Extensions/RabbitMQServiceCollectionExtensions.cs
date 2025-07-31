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

            // Registrar handlers para eventos
            services.AddScoped<CustomerCreatedEventHandler>();
            services.AddScoped<AccountRegisteredEventHandler>();
            services.AddScoped<SecureDocumentAccessRequestedEventHandler>();
            services.AddScoped<DocumentReadyToSealEventHandler>();

            // CAMBIO CRÍTICO: SIEMPRE usar EventBusRabbitMq
            // El EventBusRabbitMq ahora maneja la reconexión automática y diferida
            services.AddSingleton<IEventBus, EventBusRabbitMq>();

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
        try
        {
            var bus = app.Services.GetRequiredService<IEventBus>();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            if (bus is EventBusRabbitMq rabbitmqBus)
            {
                logger.LogInformation("🔧 Configurando suscripciones RabbitMQ INMEDIATAMENTE...");

                // CRÍTICO: Configurar todas las suscripciones SIN DELAY
                // Estas suscripciones se almacenan y se crearán los consumidores cuando RabbitMQ esté disponible
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

                logger.LogInformation(
                    "✅ Suscripciones RabbitMQ configuradas - se activarán cuando RabbitMQ esté disponible"
                );

                // Verificación de estado en segundo plano
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000); // Dar tiempo para que se complete la conexión

                    if (rabbitmqBus.IsConnected)
                    {
                        logger.LogInformation(
                            "🎯 RabbitMQ conectado - consumidores activos y procesando mensajes"
                        );
                    }
                    else
                    {
                        logger.LogInformation(
                            "⏳ RabbitMQ no conectado aún - se reconectará automáticamente"
                        );
                    }
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
}
