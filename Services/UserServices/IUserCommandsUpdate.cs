using Commons;
using DTOs.UsersDTOs;


namespace Services.UserServices;

public interface IUserCommandsUpdate
{
    Task<ApiResponse<bool>> Update(UserDTO_Only userDTO);
   Task<ApiResponse<bool>>EnableUserAsync(int userId);
    Task<ApiResponse<bool>> DisableUserAsync(int userId);

}