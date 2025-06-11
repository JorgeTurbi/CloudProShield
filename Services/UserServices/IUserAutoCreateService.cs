using Commons;
using DTOs.UsersDTOs;

namespace Services.UserServices;

public interface IUserAutoCreateService
{
    Task<ApiResponse<bool>> CreateFromAuthServiceAsync(
        UserAutoCreateDTO userDTO,
        CancellationToken cancellationToken = default
    );
}
