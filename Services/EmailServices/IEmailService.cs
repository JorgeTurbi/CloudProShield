

namespace Services.EmailServices;
public interface IEmailService
{
    Task SendConfirmationEmailAsync(string email, string token);
}
