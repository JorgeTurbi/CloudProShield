using System.Text;
using CloudShield.Middlewares;
using CloudShield.Repositories.Users;
using CloudShield.Services.FileSystemRead_Repository;
using CloudShield.Services.FileSystemServices;
using CloudShield.Services.OperationStorage;
using Commons.Utils;
using DataContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Contracts.Events;
using RabbitMQ.Integration.Handlers;
using RabbitMQ.Messaging;
using RabbitMQ.Messaging.Rabbit;
using RazorLight;
using Reponsitories.PermissionsValidate_Repository;
using Reponsitories.Roles_Repository;
using Repositories.Address_Repository;
using Repositories.CountriesRepository;
using Repositories.Permissions_Repository;
using Repositories.PermissionsDelete_Repository;
using Repositories.PermissionsUpdate_Repository;
using Repositories.RolePermissions_Repository;
using Repositories.Roles_Repository;
using Repositories.RoleUpdate_Repository;
using Repositories.Session_Repository;
using Repositories.States_Repository;
using Repositories.Users;
using Scalar.AspNetCore;
using Serilog;
using Services.AddressServices;
using Services.CountryServices;
using Services.EmailServices;
using Services.Permissions;
using Services.RolePermissions;
using Services.Roles;
using Services.SessionServices;
using Services.StateServices;
using Services.TokenServices;
using Services.UserServices;
using Session_Repository;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IUserCommandCreate, UserLib>();
builder.Services.AddScoped<IUserCommandsUpdate, UserUpdate_Repository>();
builder.Services.AddScoped<IUserCommandDelete, UserDelete_Repository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAddress, AddressLib>();
builder.Services.AddScoped<IUserCommandRead, UserRead>();
builder.Services.AddScoped<IUserCommandRead, UserRead>();
builder.Services.AddScoped<IValidateRoles, RolesValidate_Repository>();
builder.Services.AddScoped<ICreateCommandRoles, RolesLib>();
builder.Services.AddScoped<IReadCommandRoles, RolesRead_Repository>();
builder.Services.AddScoped<IUpdateCommandRoles, RoleUpdate_Repository>();
builder.Services.AddScoped<IDeleteCommandRole, RolesDelete_Repository>();
builder.Services.AddScoped<IValidatePermissions, PermissionsValidate_Repository>();
builder.Services.AddScoped<IReadCommandPermissions, PermissionsRead_Repository>();
builder.Services.AddScoped<ICreateCommandPermissions, PermissionsLib>();
builder.Services.AddScoped<IUpdateCommandPermissions, PermissionsUpdate_Repository>();
builder.Services.AddScoped<IDeleteCommandPermissions, PermissionsDelete_Repository>();
builder.Services.AddScoped<IReadCommandRolePermissions, RolePermissionsRead_Repository>();
builder.Services.AddScoped<IUpdateCommandRolePermissions, RolePermissionsUpdate_Repository>();
builder.Services.AddScoped<ICreateCommandRolePermissions, RolePermissionsLib>();
builder.Services.AddScoped<IDeleteCommandRolePermissions, RolePermissionsDelete_Repository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<UserPassword_Repository>();
builder.Services.AddScoped<IUserForgotPassword, UserForgotPassword_Repository>();
builder.Services.AddScoped<ISessionCommandCreate, SessionCreate_Repository>();
builder.Services.AddScoped<ISessionCommandRead, SessionRead_Repository>();
builder.Services.AddScoped<ISessionCommandUpdate, SessionUpdate_Repository>();
builder.Services.AddScoped<IReadCommandCountries, CountriesRead_Repository>();
builder.Services.AddScoped<IReadCommandStates, StatesRead_Repository>();
builder.Services.AddScoped<ISessionValidationService, SessionValidation_Repository>();
builder.Services.AddScoped<IStorageService, LocalDiskStorageService>();
builder.Services.AddScoped<IFileSystemReadService, FileSystemRead_Repository>();
builder.Services.AddScoped<IFolderProvisioner>(sp =>
    (IFolderProvisioner)sp.GetRequiredService<IStorageService>()
);

// Handlers
builder.Services.AddScoped<CustomerCreatedEventHandler>();

// RabbitMQ
builder.Services.AddSingleton<IEventBus, EventBusRabbitMq>();

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

// path to the log folder
var logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "LogsApplication");

// create log folder if it does not exists
if (!Directory.Exists(logFolderPath))
{
    Directory.CreateDirectory(logFolderPath);
}

// configure serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logFolderPath, "LogsApplication-.txt"),
        rollingInterval: RollingInterval.Day
    )
    .CreateLogger();

// Use Serilog
// builder.Host.UseSerilog();

var app = builder.Build();

var bus = app.Services.GetRequiredService<IEventBus>();
bus.Subscribe<CustomerCreatedEvent, CustomerCreatedEventHandler>(
    routingKey: "CustomerCreatedEvent"
);

// Configure the HTTP request pipeline.
// ✅ SWAGGER CON SWASHBUCKLE
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "Services TaxCloud V1");
    o.RoutePrefix = "swagger";
});

// ✅ REDOC Y SCALAR SOLO EN DESARROLLO SI QUIERES
if (app.Environment.IsDevelopment())
{
    // Estas herramientas también necesitan endpoints de Swagger
    app.UseReDoc(option =>
    {
        option.SpecUrl("/swagger/v1/swagger.json");
    });
    // Comentar Scalar por ahora para evitar conflictos
    // app.MapScalarApiReference();
}

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

app.Run();
