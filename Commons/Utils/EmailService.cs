namespace Commons.Utils;

using Microsoft.Extensions.Configuration;
using Services.EmailServices;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService : IEmailService
{
    private readonly IConfiguration _cfg;
    private readonly SmtpClient _client;
    private readonly string _from;

    public EmailService(IConfiguration cfg)
    {
        _cfg = cfg;
        _from = _cfg["EmailSettings:From"]!;
        _client = BuildSmtpClient();
    }

    private SmtpClient BuildSmtpClient()
    {
        var host = _cfg["EmailSettings:SmtpHost"];
        var port = int.Parse(_cfg["EmailSettings:SmtpPort"]!);
        var user = _cfg["EmailSettings:SmtpUser"];
        var pass = _cfg["EmailSettings:SmtpPass"];
        var ssl = bool.Parse(_cfg["EmailSettings:EnableSsl"] ?? "true");

        return new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = ssl
        };
    }

    /* ---------- método genérico para enviar emails ---------- */
    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var mail = new MailMessage(_from, to, subject, htmlBody) { IsBodyHtml = true };
        await _client.SendMailAsync(mail);
    }

    /* ---------- 1) Confirmación de cuenta ---------- */
    public async Task SendConfirmationEmailAsync(string email, string token)
    {
        var confirmUrl = $"{_cfg["Frontend:BaseUrl"]}/confirm?token={token}";
        var html = $"""
                <p>¡Hola!</p>
                <p>Gracias por registrarte. Confirma tu cuenta haciendo clic en el enlace:</p>
                <p><a href="{confirmUrl}">Confirmar cuenta</a></p>
                <p>Si no fuiste tú, ignora este mensaje.</p>
                """;
        await SendAsync(email, "Confirma tu cuenta – CloudShield", html);
    }

    /* ---------- 2) Link para restablecer contraseña ---------- */
    public async Task SendResetLinkAsync(string to, string resetLink)
    {
        var html = $"""
                <p>Has solicitado restablecer tu contraseña.</p>
                <p>Haz clic en el enlace:</p>
                <p><a href="{resetLink}">Restablecer contraseña</a></p>
                <p>El enlace expirará en 1 hora.</p>
                """;
        await SendAsync(to, "Restablecimiento de contraseña", html);
    }

    /* ---------- 3) OTP ---------- */
    public async Task SendOtpAsync(string to, string otp)
    {
        var html = $"""
                <p>Tu código OTP es:</p>
                <h2 style="letter-spacing:4px;">{otp}</h2>
                <p>Válido por 5 minutos.</p>
                """;
        await SendAsync(to, "Código OTP – CloudShield", html);
    }

    /* ---------- 5) Notificación de contraseña cambiada ---------- */
    public async Task SendPasswordChangedAsync(string to, string ipInfo)
    {
        var html = $"""
                <p>Tu contraseña se ha actualizado correctamente.</p>
                <p>IP / ubicación: {ipInfo}</p>
                <p>Fecha: {DateTime.UtcNow:g} UTC</p>
                """;
        await SendAsync(to, "Contraseña actualizada", html);
    }
}
