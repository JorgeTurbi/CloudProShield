using Commons;

namespace Services.UserServices;

public interface IUserCommandDelete
{
    Task<ApiResponse<bool>> Delete(int UserId);
}