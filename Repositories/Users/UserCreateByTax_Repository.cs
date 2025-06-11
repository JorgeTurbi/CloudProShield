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
            // Generar contraseña temporal
            var plainPwd = CryptoHelper.Generate();

            // Preparar DTO con datos del AuthService
            var dto = new UserAutoCreateDTO
            {
                Id = e.UserId, // IMPORTANTE: Usar el mismo GUID
                Name = !e.IsCompany ? e.Name : (e.FullName ?? e.CompanyName ?? "Company"),
                SurName = !e.IsCompany ? e.LastName : (e.CompanyName ?? string.Empty),
                Dob = !e.IsCompany ? null : DateTime.UtcNow.AddYears(-1), // Para companies, fecha ficticia
                Email = e.Email,
                PlainPassword = plainPwd, // Será hasheada en el servicio
                Phone = e.Phone ?? "",
                IsCompany = e.IsCompany,
                CompanyName = e.CompanyName,
                FullName = e.FullName,
                Domain = e.Domain,
                CountryId = 220, // Valores por defecto
                StateId = 1,
                City = "Alabama",
                Street = "420 Fourth Ave",
                Line = "Apt 1",
                ZipCode = "11582",
                Plan = "basic",
            };

            // Crear usuario usando el nuevo servicio
            var result = await _userAutoCreate.CreateFromAuthServiceAsync(dto, ct);

            if (!result.Success)
            {
                _log.LogError(
                    "Failed to create user from AuthService for {Email}: {Message}",
                    e.Email,
                    result.Message
                );
                return;
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
}
