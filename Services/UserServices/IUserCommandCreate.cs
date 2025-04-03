using Commons;
using DTOs;

namespace Services.UserServices;

public interface IUserCommandCreate
{

        Task<ApiResponse<bool>> AddNew(UserDTO userDTO,CancellationToken cancellationToken=default);
      
 
}
