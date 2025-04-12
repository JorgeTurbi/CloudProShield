namespace Application.DTOs.Auth;

public record ResetPasswordDTO(string Email, string Token, string Otp, string NewPassword);