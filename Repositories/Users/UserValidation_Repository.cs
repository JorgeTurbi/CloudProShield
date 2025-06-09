using Commons;
using Commons.Hash;
using DataContext;
using Entities.Users;
using Microsoft.EntityFrameworkCore;
using Services.UserServices;

namespace Repositories.Users;

public class UserValidation_Repository : IUserValidationService
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<UserValidation_Repository> _logger;
  public UserValidation_Repository(ApplicationDbContext context, ILogger<UserValidation_Repository> logger)
  {
    _context = context;
    _logger = logger;
  }
  public async Task<ApiResponse<User>> ValidateUserForLogin(string email, string password)
  {
    try
    {
      var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);

      if (user == null)
        return new ApiResponse<User>(false, "Invalid email or password");

      // Verificar password usando tu PasswordHasher
      bool isPasswordValid = PasswordHasher.VerifyPassword(user.Password, password);
      if (!isPasswordValid)
        return new ApiResponse<User>(false, "Invalid email or password");

      // Verificar que el usuario esté activo
      if (!user.IsActive)
        return new ApiResponse<User>(false, "Your account has been deactivated. Please contact support.");

      // Verificar que la cuenta esté confirmada
      if (!user.Confirm)
        return new ApiResponse<User>(false, "Please confirm your email address before logging in. Check your inbox for the confirmation email.");

      return new ApiResponse<User>(true, "User validated successfully", user);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error validating user for login");
      return new ApiResponse<User>(false, "An error occurred during validation");
    }
  }

  public async Task<ApiResponse<bool>> ValidateUserForRegistration(string email)
  {
    try
    {
      var existingUser = await _context.User.FirstOrDefaultAsync(u => u.Email == email);

      if (existingUser != null)
        return new ApiResponse<bool>(false, "An account with this email already exists");

      return new ApiResponse<bool>(true, "Email is available for registration");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error validating user for registration");
      return new ApiResponse<bool>(false, "An error occurred during validation");
    }
  }
}