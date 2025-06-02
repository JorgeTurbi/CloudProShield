using Commons;
using DTOs.UsersDTOs;

namespace Services.UserServices;

public interface IUserCommandCreate
{
        Task<ApiResponse<bool>> AddNew(UserDTO userDTO, string originUrl = null, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> ConfirmEmailAsync(string token);

}
