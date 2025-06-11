using CloudShield.Repositories.Users;
using CloudShield.Services.FileSystemRead_Repository;
using CloudShield.Services.FileSystemServices;
using CloudShield.Services.OperationStorage;
using Commons.Utils;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Integration.Handlers;
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

namespace CloudShield.Repositories.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomRepostories(this IServiceCollection services)
    {
        services.AddScoped<IUserCommandCreate, UserLib>();
        services.AddScoped<IUserCreateByTaxPro, UserCreateByTax_Repository>();
        services.AddScoped<IUserAutoCreateService, UserAutoCreateService>();
        services.AddScoped<IUserCommandsUpdate, UserUpdate_Repository>();
        services.AddScoped<IUserCommandDelete, UserDelete_Repository>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAddress, AddressLib>();
        services.AddScoped<IUserCommandRead, UserRead>();
        services.AddScoped<IUserCommandRead, UserRead>();
        services.AddScoped<IUserValidationService, UserValidation_Repository>();
        services.AddScoped<IValidateRoles, RolesValidate_Repository>();
        services.AddScoped<ICreateCommandRoles, RolesLib>();
        services.AddScoped<IReadCommandRoles, RolesRead_Repository>();
        services.AddScoped<IUpdateCommandRoles, RoleUpdate_Repository>();
        services.AddScoped<IDeleteCommandRole, RolesDelete_Repository>();
        services.AddScoped<IValidatePermissions, PermissionsValidate_Repository>();
        services.AddScoped<IReadCommandPermissions, PermissionsRead_Repository>();
        services.AddScoped<ICreateCommandPermissions, PermissionsLib>();
        services.AddScoped<IUpdateCommandPermissions, PermissionsUpdate_Repository>();
        services.AddScoped<IDeleteCommandPermissions, PermissionsDelete_Repository>();
        services.AddScoped<IReadCommandRolePermissions, RolePermissionsRead_Repository>();
        services.AddScoped<IUpdateCommandRolePermissions, RolePermissionsUpdate_Repository>();
        services.AddScoped<ICreateCommandRolePermissions, RolePermissionsLib>();
        services.AddScoped<IDeleteCommandRolePermissions, RolePermissionsDelete_Repository>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<UserPassword_Repository>();
        services.AddScoped<IUserForgotPassword, UserForgotPassword_Repository>();
        services.AddScoped<ISessionCommandCreate, SessionCreate_Repository>();
        services.AddScoped<ISessionCommandRead, SessionRead_Repository>();
        services.AddScoped<ISessionCommandUpdate, SessionUpdate_Repository>();
        services.AddScoped<IReadCommandCountries, CountriesRead_Repository>();
        services.AddScoped<IReadCommandStates, StatesRead_Repository>();
        services.AddScoped<ISessionValidationService, SessionValidation_Repository>();
        services.AddScoped<IStorageService, LocalDiskStorageService>();
        services.AddScoped<IStorageServiceUser, LocalDiskStorageServiceUser>();
        services.AddScoped<IFileSystemReadService, FileSystemRead_Repository>();
        services.AddScoped<IFileSystemReadServiceUser, FileSystemRead_RepositoryUser>();
        services.AddScoped<IFolderProvisioner>(sp =>
            (IFolderProvisioner)sp.GetRequiredService<IStorageService>()
        );
        services.AddScoped<IFolderProvisionerUser>(sp =>
            (IFolderProvisionerUser)sp.GetRequiredService<IStorageServiceUser>()
        );

        // Handlers for RabbitMQ events
        services.AddScoped<CustomerCreatedEventHandler>();
        services.AddScoped<AccountRegisteredEventHandler>();

        return services;
    }
}
