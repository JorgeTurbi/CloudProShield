namespace Services.EmailServices;
public interface IEmailService
{
    Task SendConfirmationEmailAsync(string email, string toke, string originUrl);
    Task SendAccountConfirmedAsync(string email);
    Task SendResetLinkAsync(string to, string resetLink);
    Task SendOtpAsync(string to, string otp);
    Task SendPasswordChangedAsync(string to, string ipInfo);
    Task SendLoginNotificationAsync(string to, string ipInfo, string device);
    Task SendAsync(string to, string subject, string htmlBody);
    Task SendTemplatedEmailAsync<T>(string to, string subject, string template, T model);
}
