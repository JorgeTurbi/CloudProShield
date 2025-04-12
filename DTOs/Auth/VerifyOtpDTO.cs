namespace Application.DTOs.Auth;

public record VerifyOtpDTO(string Email, string Token, string Otp);