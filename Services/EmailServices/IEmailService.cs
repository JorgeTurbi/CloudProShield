namespace Services.EmailServices;
public interface IEmailService
{
    Task SendConfirmationEmailAsync(string email, string token);
    Task SendResetLinkAsync(string to, string resetLink);
    Task SendOtpAsync(string to, string otp);
    Task SendPasswordChangedAsync(string to, string ipInfo);
    Task SendAsync(string to, string subject, string htmlBody);
}
