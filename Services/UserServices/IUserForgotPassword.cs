using Commons;

namespace Services.UserServices
{
    public interface IUserForgotPassword
    {
        Task<ApiResponse<string>> ForgotPasswordAsync(string email, string origin);
        Task<ApiResponse<string>> SendOtpAsync(string email, string token);
        Task<ApiResponse<string>> VerifyOtpAsync(string email, string token, string otp);
        Task<ApiResponse<string>> ResetPasswordAsync(string email, string token, string otp, string newPassword, string ipAddress);
    }
}