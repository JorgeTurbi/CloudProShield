using Commons;
using DTOs;

namespace Services.UserServices;

public interface IUserCommandRead
{
  Task<ApiResponse<List<UserDTO>>> GetAllUsers();
  Task<ApiResponse<UserDTO>> GetUserById(int id);
}