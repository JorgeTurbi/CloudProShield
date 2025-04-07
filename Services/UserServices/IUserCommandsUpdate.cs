using Commons;
using DTOs.UsersDTOs;


namespace Services.UserServices;

public interface IUserCommandsUpdate
{
    Task<ApiResponse<bool>> Update(UserDTO userDTO);
}