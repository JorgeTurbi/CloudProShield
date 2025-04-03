using CloudShield.Repositories.Users;
using DataContext;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Services.UserServices;

var builder = WebApplication.CreateBuilder(args);
// Leer la cadena desde la configuración
var connectionString = builder.Configuration.GetConnectionString("LogConnectionString");
var logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "LogsApplication");

builder.Services.AddScoped<IUserCommandCreate, UserLib>();
builder.Services.AddScoped<IUserCommandsUpdate, UserLib>();
builder.Services.AddScoped<IUserCommandRead, UserLib>();

if (!Directory.Exists(logFolderPath))
{
  Directory.CreateDirectory(logFolderPath);
}

//todo LogConfiguration
if (!Directory.Exists(logFolderPath))
{
  Directory.CreateDirectory(logFolderPath);
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)  // Lee la configuración desde appsettings.json
    .Enrich.FromLogContext()
    .WriteTo.Console()  // Enviar logs a la consola
    .WriteTo.File(Path.Combine(logFolderPath, "logApplicatios-.txt"), rollingInterval: RollingInterval.Day)  // Guardar logs en archivo diario
    .CreateLogger();

builder.Services.AddControllers();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program));
// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
  options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.UseSwaggerUI(o =>
  {
    o.SwaggerEndpoint("/openapi/v1.json", "Services TaxCloud V1");
  });

  app.UseReDoc(option =>
  {
    option.SpecUrl("/openapi/v1.json");
  });
  app.MapScalarApiReference();
}
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

