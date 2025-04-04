using Commons;
using DTOs;
using Users;

namespace Services.UserServices;

public interface IUserCommandRead
{
  Task<ApiResponse<List<UserListDTO>>> GetAllUsers();
  Task<ApiResponse<UserDetailDTO>> GetUserById(int id);
}