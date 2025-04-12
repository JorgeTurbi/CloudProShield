using Commons;
using DTOs.UsersDTOs;
using Microsoft.AspNetCore.Identity.Data;

namespace Services.UserServices;

public interface IUserCommandCreate
{

        Task<ApiResponse<bool>> AddNew(UserDTO userDTO,CancellationToken cancellationToken=default);
        Task<ApiResponse<bool>> ConfirmEmailAsync(string token);

      
 
}
