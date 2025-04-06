using CloudShield.Repositories.Users;
using DataContext;
using Microsoft.EntityFrameworkCore;
using Repositories.Address_Repository;
using Scalar.AspNetCore;
using Serilog;
using Services.AddressServices;
using Services.UserServices;


var builder = WebApplication.CreateBuilder(args);
// Leer la cadena desde la configuración
var connectionString = builder.Configuration.GetConnectionString("LogConnectionString");
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
        Path.Combine(logFolderPath, "logApplication-.txt"),
        rollingInterval: RollingInterval.Day
    )
    .CreateLogger();

    // Use Serilog
builder.Host.UseSerilog();

//todo services configuration
    builder.Services.AddScoped<IUserCommandCreate,UserLib>();
builder.Services.AddScoped<IAddress,AddressLib>();


builder.Services.AddControllers();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program));
// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options=>
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
   app.UseSwaggerUI(o=>{
    o.SwaggerEndpoint("/openapi/v1.json", "Services TaxCloud V1" );
   } );

   app.UseReDoc(option=>{
    option.SpecUrl("/openapi/v1.json");
   });
   app.MapScalarApiReference();
}
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

