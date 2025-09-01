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
using RabbitMQ.Messaging;
using RabbitMQ.Messaging.Health;
using RabbitMQ.Messaging.Rabbit;
using RazorLight;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
// ✅ CONFIGURACIÓN DE LOGGING
builder.Logging.ClearProviders();

var logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "LogsApplication");
if (!Directory.Exists(logFolderPath))
{
    Directory.CreateDirectory(logFolderPath);
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.File(
        Path.Combine(logFolderPath, "LogsApplication-.txt"),
        rollingInterval: RollingInterval.Day
    )
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger, dispose: true);


// var templatesRoot = builder.Configuration["Mail:TemplatesPath"]
//     ?? Path.Combine(builder.Environment.ContentRootPath, "Mail", "Templates");

//     builder.Services.AddSingleton(sp =>
// {
//     if (!Directory.Exists(templatesRoot))
//         throw new DirectoryNotFoundException($"Mail templates folder not found: {templatesRoot}");

//     var engine = new RazorLight.RazorLightEngineBuilder()
//         .UseFileSystemProject(templatesRoot)
//         .UseMemoryCachingProvider()
//         .Build();

//     return engine;
// });

Console.WriteLine("🚀 Iniciando CloudProShield API...");
Console.WriteLine($"📁 Logs guardados en: {logFolderPath}");

// ✅ CONFIGURACIÓN DE LÍMITES DE ARCHIVOS
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024; // 5 GB
});

builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 5L * 1024 * 1024 * 1024; // 5 GB
});

// ✅ CORS POLICY
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(
//         "AllowAllOrigins",
//         builder =>
//         {
//             builder.WithOrigins("https://cloud.taxprosuite.com/",
//              "https://go.taxprosuite.com/",
//               "https://taxprosuite.com")
//             .AllowAnyMethod().AllowAnyHeader();
//         }
//     );
// });

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowSpecific", p => p
        .WithOrigins(allowedOrigins)         // ¡dominios exactos, sin slash!
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()                  // si usas cookies o auth con credenciales
    );
});

// ✅ JWT AUTHENTICATION
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(jwtSettings!.SecretKey);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// ✅ SERVICIOS PERSONALIZADOS
builder.Services.AddCustomRepostories();

// ✅ RABBITMQ CON RECONEXIÓN AUTOMÁTICA (NO BLOQUEANTE)
builder.Services.AddRabbitMQEventBus(builder.Configuration);

// ✅ RAZOR LIGHT ENGINE
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var templateRoot = Path.Combine(env.ContentRootPath, "Mail", "Templates");

    return new RazorLightEngineBuilder()
        .UseFileSystemProject(templateRoot)
        .UseMemoryCachingProvider()
        .Build();
});

// ✅ CONTROLADORES Y AUTOMAPPER
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddAutoMapper(typeof(FileSystemProfile));

// ✅ ENTITY FRAMEWORK
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ✅ SWAGGER/OPENAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Services TaxCloud API", Version = "v1" });

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
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // <- tu DbContext concreto
    db.Database.Migrate(); // aplica migraciones pendientes
}
// ✅ PIPELINE DE MIDDLEWARES
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "Services TaxCloud V1");
    o.RoutePrefix = "swagger";
});

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
app.UseCors("AllowSpecific");  

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

// ✅ CONFIGURAR SUSCRIPCIONES RABBITMQ (NO BLOQUEANTE)
app.ConfigureRabbitMQSubscriptions();

// ✅ HEALTH CHECK ENDPOINTS
app.MapGet(
    "/health",
    (IServiceProvider services) =>
    {
        var bus = services.GetRequiredService<IEventBus>();
        var healthService = services.GetService<IRabbitMQHealthService>();

        return Results.Ok(
            new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                rabbitmq = new
                {
                    enabled = healthService != null,
                    healthy = healthService?.IsHealthy ?? false,
                    eventbus_type = bus.GetType().Name,
                    mode = bus is EventBusRabbitMq ? "Full" : "Degraded",
                },
            }
        );
    }
);

app.MapGet(
    "/health/rabbitmq",
    async (IServiceProvider services) =>
    {
        var healthService = services.GetService<IRabbitMQHealthService>();
        if (healthService == null)
        {
            return Results.Json(
                new { status = "Disabled", message = "RabbitMQ not configured" },
                statusCode: 503
            );
        }

        var isHealthy = await healthService.CheckHealthAsync();
        if (isHealthy)
        {
            return Results.Ok(
                new { status = "Healthy", message = "RabbitMQ connected and operational" }
            );
        }

        return Results.Json(
            new { status = "Unavailable", message = "RabbitMQ not reachable" },
            statusCode: 503
        );
    }
);

// ✅ MENSAJES DE INICIO
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
    Console.WriteLine($"🐰 RabbitMQ Health: {url}/health/rabbitmq");
}

Console.WriteLine(new string('=', 60));

// ✅ ABRIR SWAGGER EN DESARROLLO
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
