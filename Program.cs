using System.Text;
using CloudShield.Repositories.Users;
using Commons.Utils;
using DataContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Reponsitories.PermissionsValidate_Repository;
using Reponsitories.Roles_Repository;
using Repositories.Address_Repository;
using Repositories.Permissions_Repository;
using Repositories.PermissionsDelete_Repository;
using Repositories.PermissionsUpdate_Repository;
using Repositories.RolePermissions_Repository;
using Repositories.Roles_Repository;
using Repositories.RoleUpdate_Repository;
using Repositories.Users;
using Scalar.AspNetCore;
using Serilog;
using Services.AddressServices;
using Services.EmailServices;
using Services.Permissions;
using Services.RolePermissions;
using Services.Roles;
using Services.SessionServices;
using Services.TokenServices;
using Services.UserServices;
using Session_Repository;

var builder = WebApplication.CreateBuilder(args);

//todo add cors policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var jwtSettin = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{

    var key = Encoding.UTF8.GetBytes(jwtSettin.SecretKey);


    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero // sin tolerancia de expiración
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

builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program));
// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// // path to the log folder
var logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "LogsApplication");

// // create log folder if it does not exists
if (!Directory.Exists(logFolderPath))
{
    Directory.CreateDirectory(logFolderPath);
}

// // configure serilog
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

// Configure the HTTP request pipeline.
if (app.Environment.IsProduction())
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
//  app.MapOpenApi();
//    app.UseSwaggerUI(o=>{
//     o.SwaggerEndpoint("/openapi/v1.json", "Services TaxCloud V1" );
//    } );

//    app.UseReDoc(option=>{
//     option.SpecUrl("/openapi/v1.json");
//    });
//    app.MapScalarApiReference();


app.UseCors("AllowAllOrigins");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();