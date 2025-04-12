namespace Commons.Utils;

using Microsoft.Extensions.Configuration;
using Services.EmailServices;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendConfirmationEmailAsync(string email, string token)
    {
        var fromEmail = _configuration["EmailSettings:From"];
        var smtpHost = _configuration["EmailSettings:SmtpHost"];
        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
        var smtpUser = _configuration["EmailSettings:SmtpUser"];
        var smtpPass = _configuration["EmailSettings:SmtpPass"];

        var confirmUrl = $"https://tu-dominio.com/confirm?token={token}";
        var subject = "Confirma tu cuenta";
        var body = $"Hola!<br><br>Gracias por registrarte.<br>Por favor confirma tu cuenta haciendo clic en el siguiente enlace:<br><br>" +
                   $"<a href='{confirmUrl}'>Confirmar Cuenta</a><br><br>Si no te registraste, ignora este mensaje.";

        var message = new MailMessage(fromEmail, email, subject, body)
        {
            IsBodyHtml = true
        };

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        await client.SendMailAsync(message);
    }
}
