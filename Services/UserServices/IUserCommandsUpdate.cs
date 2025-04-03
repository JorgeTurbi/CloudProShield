using Commons;
using DTOs;

namespace Services.UserServices;

public interface IUserCommandsUpdate
{
    Task<ApiResponse<bool>> Update(UserDTO userDTO);
}