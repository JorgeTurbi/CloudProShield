using Commons;
using Entities.Users;

namespace Services.UserServices;

public interface IUserValidationService
{
  Task<ApiResponse<User>> ValidateUserForLogin(string email, string password);
  Task<ApiResponse<bool>> ValidateUserForRegistration(string email);
}