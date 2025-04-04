using Commons;
using DTOs;

namespace Services.UserServices;

public interface IUserCommandCreate
{
  Task<ApiResponse<bool>> AddNew(UserCreateUpdateDTO userDTO, CancellationToken cancellationToken = default);
}
