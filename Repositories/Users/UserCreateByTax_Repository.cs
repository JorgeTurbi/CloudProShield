using CloudShield.Commons.Utils;
using DTOs.UsersDTOs;
using RabbitMQ.Contracts.Events;
using Services.EmailServices;
using Services.UserServices;

public class UserCreateByTax_Repository : IUserCreateByTaxPro
{
    private readonly IUserAutoCreateService _userAutoCreate;
    private readonly IEmailService _mail;
    private readonly ILogger<UserCreateByTax_Repository> _log;
    private readonly IConfiguration _cfg;

    public UserCreateByTax_Repository(
        IUserAutoCreateService userAutoCreate,
        IEmailService mail,
        IConfiguration cfg,
        ILogger<UserCreateByTax_Repository> log
    )
    {
        _userAutoCreate = userAutoCreate;
        _mail = mail;
        _cfg = cfg;
        _log = log;
    }

    public async Task ProvisionAsync(AccountRegisteredEvent e, CancellationToken ct)
    {
        try
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(e.Email))
            {
                _log.LogError("Invalid email in AccountRegisteredEvent: {Email}", e.Email);
                return;
            }

            if (e.UserId == Guid.Empty)
            {
                _log.LogError("Invalid UserId in AccountRegisteredEvent: {UserId}", e.UserId);
                return;
            }

            // Generar contraseña temporal
            var plainPwd = CryptoHelper.Generate();

            // Determinar qué dirección usar
            var addressToUse = DetermineAddress(e);

            // Validar que tenemos dirección válida
            if (addressToUse == null)
            {
                _log.LogError(
                    "No valid address found for {Email} - CompanyAddress: {HasCompany}, UserAddress: {HasUser}, IsCompany: {IsCompany}",
                    e.Email,
                    e.CompanyAddress != null,
                    e.UserAddress != null,
                    e.IsCompany
                );
                return;
            }

            _log.LogInformation(
                "Using address for {Email}: Country={Country}, State={State}, City={City}, Street={Street}",
                e.Email,
                addressToUse.CountryId,
                addressToUse.StateId,
                addressToUse.City ?? "NULL",
                addressToUse.Street ?? "NULL"
            );

            // Preparar DTO con datos del AuthService
            var dto = new UserAutoCreateDTO
            {
                Id = e.UserId, // IMPORTANTE: Usar el mismo GUID
                Name = DetermineName(e),
                SurName = DetermineSurName(e),
                Dob = e.IsCompany ? DateTime.UtcNow.AddYears(-1) : DateTime.UtcNow.AddYears(-25),
                Email = e.Email,
                PlainPassword = plainPwd, // Será hasheada en el servicio
                Phone = e.Phone ?? string.Empty,
                IsCompany = e.IsCompany,
                CompanyName = e.CompanyName,
                FullName = e.FullName,
                Domain = e.Domain,
                CountryId = addressToUse.CountryId,
                StateId = addressToUse.StateId,
                City = GetValidValue(addressToUse.City, "Miami"),
                Street = GetValidValue(addressToUse.Street, "Main Street"),
                Line = addressToUse.Line,
                ZipCode = GetValidValue(addressToUse.ZipCode, "33101"),
                Plan = DeterminePlan(e.IsCompany),
            };

            // Log detallado para debugging
            _log.LogInformation(
                "Creating user from AuthService - Email: {Email}, IsCompany: {IsCompany}, Address: {Country}/{State}/{City}",
                dto.Email,
                dto.IsCompany,
                dto.CountryId,
                dto.StateId,
                dto.City
            );

            // Crear usuario usando el nuevo servicio
            var result = await _userAutoCreate.CreateFromAuthServiceAsync(dto, ct);

            if (!result.Success)
            {
                _log.LogWarning(
                    "User creation result: {Success} - {Message} for {Email}",
                    result.Success,
                    result.Message,
                    e.Email
                );

                // CRÍTICO: Solo enviar email si el usuario NO existía y se creó exitosamente
                // Si result.Success = false y Message = "User already exists", NO enviar email
                if (result.Message?.Contains("already exists") == true)
                {
                    _log.LogInformation(
                        "User {Email} already exists in CloudShield. No email sent.",
                        e.Email
                    );
                    return; // EXIT - No enviar email
                }

                _log.LogError(
                    "Failed to create user from AuthService for {Email}: {Message}",
                    e.Email,
                    result.Message
                );
                return; // EXIT - Error real
            }

            // Enviar correo con credenciales temporales
            var frontUrl = _cfg["Frontend:BaseUrl"] ?? "https://app.cloudshield.io";
            await _mail.SendAutoCreatedAccountAsync(e.Email, plainPwd, $"{frontUrl}/login");

            _log.LogInformation(
                "Account {Email} (IsCompany={IsCompany}) successfully provisioned from AuthService",
                e.Email,
                e.IsCompany
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error provisioning account from AuthService for {Email}", e.Email);
        }
    }

    /// <summary>
    /// Determina el nombre según el tipo de cuenta
    /// </summary>
    private static string DetermineName(AccountRegisteredEvent e)
    {
        if (e.IsCompany)
        {
            // Para empresas: usar CompanyName, o FullName como fallback
            return e.CompanyName ?? e.FullName ?? "Company";
        }
        else
        {
            // Para individuales: usar Name del administrador
            return e.Name ?? "User";
        }
    }

    /// <summary>
    /// Determina el apellido según el tipo de cuenta
    /// </summary>
    private static string DetermineSurName(AccountRegisteredEvent e)
    {
        if (e.IsCompany)
        {
            // Para empresas: usar string vacío o un sufijo
            return "LLC"; // O string.Empty si prefieres
        }
        else
        {
            // Para individuales: usar LastName del administrador
            return e.LastName ?? string.Empty;
        }
    }

    /// <summary>
    /// Determina el plan según el tipo de cuenta
    /// </summary>
    private static string DeterminePlan(bool isCompany)
    {
        return isCompany ? "pro" : "basic";
    }

    /// <summary>
    /// Determina qué dirección usar basándose en el tipo de cuenta
    /// </summary>
    private static AddressPayload? DetermineAddress(AccountRegisteredEvent e)
    {
        if (e.IsCompany)
        {
            // Para empresas: preferir UserAddress (del admin), fallback a CompanyAddress
            return e.UserAddress ?? e.CompanyAddress;
        }
        else
        {
            // Para individuales: preferir UserAddress, fallback a CompanyAddress
            // En individuales ambas deberían ser la misma dirección
            return e.UserAddress ?? e.CompanyAddress;
        }
    }

    /// <summary>
    /// Obtiene un valor válido o usa el fallback si es null/empty
    /// </summary>
    private static string GetValidValue(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
