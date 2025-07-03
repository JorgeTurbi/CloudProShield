using System.Text;
using CloudShield.Middlewares;
using CloudShield.Profiles.FileSystem;
using CloudShield.Repositories.Extensions;
using Commons.Utils;
using DataContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using RabbitMQ.Contracts.Events;
using RabbitMQ.Integration.Handlers;
using RabbitMQ.Messaging;
using RabbitMQ.Messaging.Rabbit;
using RazorLight;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Elimina los loggers predeterminados de ASP.NET Core
builder.Logging.ClearProviders();

var logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "LogsApplication");
if (!Directory.Exists(logFolderPath))
{
    Directory.CreateDirectory(logFolderPath);
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    // .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logFolderPath, "LogsApplication-.txt"),
        rollingInterval: RollingInterval.Day
    )
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger, dispose: true);

// ✅ MOSTRAR INFORMACIÓN DE INICIO TEMPRANO
Console.WriteLine("🚀 Iniciando CloudProShield API...");
Console.WriteLine($"📁 Logs guardados en: {logFolderPath}");

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024; // 5 GB
});

builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 5L * 1024 * 1024 * 1024; // 5 GB
});

//todo add cors policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

var jwtSettin = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(jwtSettin!.SecretKey);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero, // sin tolerancia de expiración
        };
    });

builder.Services.AddAuthorization();

//todo services configuration
builder.Services.AddCustomRepostories();

// CONFIGURACIÓN ROBUSTA DE RABBITMQ INTEGRADA
var rabbitMQEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", true);

if (rabbitMQEnabled)
{
    // Verificar conectividad a RabbitMQ al inicio
    bool isRabbitMQAvailable = false;
    try
    {
        var factory = new ConnectionFactory
        {
            HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest",
            Password = builder.Configuration["RabbitMQ:Password"] ?? "guest",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            RequestedHeartbeat = TimeSpan.FromSeconds(10),
        };

        using var testConnection = factory.CreateConnection();
        using var testChannel = testConnection.CreateModel();
        testChannel.ExchangeDeclare(
            "startup-test",
            ExchangeType.Direct,
            durable: false,
            autoDelete: true
        );

        isRabbitMQAvailable = true;
        builder.Services.AddSingleton<IEventBus, EventBusRabbitMq>();
        Console.WriteLine("✅ RabbitMQ disponible - usando EventBusRabbitMq");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  RabbitMQ no disponible: {ex.Message} - usando NoOpEventBus");
        isRabbitMQAvailable = false;
    }

    if (!isRabbitMQAvailable)
    {
        // Implementación No-Op inline
        builder.Services.AddSingleton<IEventBus>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<IEventBus>>();
            return new NoOpEventBus(logger);
        });
    }
}
else
{
    Console.WriteLine("🔕 RabbitMQ deshabilitado en configuración - usando NoOpEventBus");
    builder.Services.AddSingleton<IEventBus>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<IEventBus>>();
        return new NoOpEventBus(logger);
    });
}

builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var templateRoot = Path.Combine(env.ContentRootPath, "Mail", "Templates");

    return new RazorLightEngineBuilder()
        .UseFileSystemProject(templateRoot)
        .UseMemoryCachingProvider()
        .Build();
});

builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddAutoMapper(typeof(FileSystemProfile));

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Usar Swashbuckle para mejor compatibilidad
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Services TaxCloud API", Version = "v1" });

    // Configuración de JWT para Swagger
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// ✅ SWAGGER CON SWASHBUCKLE
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "Services TaxCloud V1");
    o.RoutePrefix = "swagger";
});

// ✅ CONFIGURACIÓN DE SUSCRIPCIONES RABBITMQ INTEGRADA
try
{
    var bus = app.Services.GetRequiredService<IEventBus>();

    if (bus is EventBusRabbitMq)
    {
        bus.Subscribe<CustomerCreatedEvent, CustomerCreatedEventHandler>("CustomerCreatedEvent");
        bus.Subscribe<AccountRegisteredEvent, AccountRegisteredEventHandler>(
            "AccountRegisteredEvent"
        );
        bus.Subscribe<
            SecureDocumentAccessRequestedEvent,
            SecureDocumentAccessRequestedEventHandler
        >("SecureDocumentAccessRequestedEvent");
        bus.Subscribe<DocumentReadyToSealEvent, DocumentReadyToSealEventHandler>(
            "DocumentReadyToSealEvent"
        );
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning("Messaging no disponible: {Message}", ex.Message);
}

// ReDoc solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseReDoc(option =>
    {
        option.SpecUrl("/swagger/v1/swagger.json");
        option.RoutePrefix = "redoc";
    });
}

app.UseSerilogRequestLogging();
app.UseCors("AllowAllOrigins");

// ✅ HTTPS REDIRECTION SOLO SI HAY PUERTO HTTPS CONFIGURADO
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(
    new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(Directory.GetCurrentDirectory(), "Mail", "Assets")
        ),
        RequestPath = "/static",
    }
);

app.UseAuthentication();
app.UseMiddleware<SessionValidationMiddleware>();
app.UseAuthorization();

app.MapControllers();

// ✅ HEALTH CHECK ENDPOINTS
app.MapGet(
    "/health",
    () =>
        Results.Ok(
            new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                rabbitmq = app.Services.GetRequiredService<IEventBus>() is EventBusRabbitMq
                    ? "Connected"
                    : "Disabled/Unavailable",
            }
        )
);

app.MapGet(
    "/health/rabbitmq",
    () =>
    {
        var bus = app.Services.GetRequiredService<IEventBus>();
        if (bus is EventBusRabbitMq)
        {
            return Results.Ok(new { status = "Healthy", message = "RabbitMQ connected" });
        }
        return Results.Json(
            new { status = "Unavailable", message = "RabbitMQ not available" },
            statusCode: 503
        );
    }
);

// ✅ MENSAJES DE INICIO PROMINENTES
Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("🌟 CLOUDPROSHIELD API INICIADA EXITOSAMENTE");
Console.WriteLine(new string('=', 60));

var urls =
    builder.Configuration["ASPNETCORE_URLS"]?.Split(';') ?? new[] { "http://localhost:5009" };
foreach (var url in urls)
{
    Console.WriteLine($"🌐 Servidor disponible en: {url}");
    Console.WriteLine($"📚 Swagger UI: {url}/swagger");
    Console.WriteLine($"❤️  Health Check: {url}/health");
}

Console.WriteLine(new string('=', 60));

// ✅ ABRIR SWAGGER AUTOMÁTICAMENTE EN DESARROLLO
if (app.Environment.IsDevelopment())
{
    var url = urls.FirstOrDefault()?.Replace("*", "localhost") ?? "http://localhost:5009";
    var swaggerUrl = $"{url}/swagger";

    try
    {
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo
            {
                FileName = swaggerUrl,
                UseShellExecute = true,
            }
        );
        Console.WriteLine($"🚀 Abriendo Swagger automáticamente: {swaggerUrl}");
    }
    catch
    {
        Console.WriteLine($"💡 Abra manualmente Swagger en: {swaggerUrl}");
    }
}

Console.WriteLine("\n✅ Presiona Ctrl+C para detener el servidor\n");

app.Run();

// ✅ IMPLEMENTACIÓN NO-OP MEJORADA
public sealed class NoOpEventBus : IEventBus
{
    private readonly ILogger<IEventBus> _logger;

    public NoOpEventBus(ILogger<IEventBus> logger)
    {
        _logger = logger;
    }

    public void Publish(string routingKey, object message)
    {
        _logger.LogDebug("Evento {RoutingKey} omitido - RabbitMQ no disponible", routingKey);
    }

    public void Subscribe<TEvent, THandler>(string routingKey)
        where THandler : IIntegrationEventHandler<TEvent>
    {
        _logger.LogDebug("Suscripción {RoutingKey} omitida - RabbitMQ no disponible", routingKey);
    }
}
