
using CloudShield.Commons.Utils;
using CloudShield.Repositories.Users;
using Commons;
using Commons.Hash;
using Services.EmailServices;
using Services.TokenServices;
using Services.UserServices;

namespace Repositories.Users;

public class UserForgotPassword_Repository : IUserForgotPassword
{
  private readonly UserPassword_Repository _repo;
  private readonly IEmailService _mail;
  private readonly IConfiguration _cfg;
  private readonly ITokenService _token;

    public UserForgotPassword_Repository(UserPassword_Repository repo, IEmailService mail, IConfiguration cfg, ITokenService token)
    {
        _repo = repo;
        _mail = mail;
        _cfg = cfg;
        _token = token;
    }
    public async Task<ApiResponse<string>> ForgotPasswordAsync(string email, string origin)
  {
    var user = await _repo.FindByEmailAsync(email);
    if (user is null) return new ApiResponse<string>(false, "User not found", null);

    user.ResetPasswordToken = _token.IssueResetToken(user.Email, TimeSpan.FromHours(1));
    user.ResetPasswordExpires = DateTime.UtcNow.AddHours(1);
    await _repo.SaveAsync();

    var link = $"{origin}/reset-password/{user.ResetPasswordToken}";
    await _mail.SendResetLinkAsync(user.Email, link);
    return new(true, "Password-reset link sent");
  }

  public async Task<ApiResponse<string>> ResetPasswordAsync(string email, string token, string otp, string newPassword, string ipAddress)
  {
    var user = await _repo.FindForResetAsync(email, token);
    if (user is null) return new(false, "Invalid or expired token");
    if (user.Otp != otp || user.OtpExpires < DateTime.UtcNow)
      return new(false, "Invalid OTP");

    user.Password = PasswordHasher.HashPassword(newPassword);
    user.ResetPasswordToken = null;
    user.ResetPasswordExpires = DateTime.MinValue;
    user.Otp = null;
    user.OtpExpires = DateTime.MinValue;
    await _repo.SaveAsync();

    await _mail.SendPasswordChangedAsync(user.Email,
        $"Tu contraseña se cambió desde {ipAddress} a las {DateTime.UtcNow:g}");
    return new(true, "Password updated");
  }

  public async Task<ApiResponse<string>> SendOtpAsync(string email, string token)
  {
    var user = await _repo.FindForResetAsync(email, token);
    if (user is null) return new(false, "Invalid or expired token");

    user.Otp = CryptoHelper.RandomOtp(6);
    user.OtpExpires = DateTime.UtcNow.AddMinutes(5);
    await _repo.SaveAsync();

    await _mail.SendOtpAsync(user.Email,
        $"Tu código OTP es: {user.Otp}");
    return new(true, "OTP sent");
  }

  public async Task<ApiResponse<string>> VerifyOtpAsync(string email, string token, string otp)
  {
    var user = await _repo.FindForResetAsync(email, token);
    if (user is null || user.OtpExpires < DateTime.UtcNow)
      return new(false, "Invalid or expired token/otp");

    if (user.Otp != otp) return new(false, "Invalid OTP");
    return new(true, "OTP valid");
  }
}