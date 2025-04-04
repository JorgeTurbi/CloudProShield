using Commons;
using DTOs;
using Users;

namespace Services.UserServices;

public interface IUserCommandRead
{
  Task<ApiResponse<List<UserListDTO>>> GetAllUsers();
  Task<ApiResponse<UserDTO>> GetUserById(int id);
}