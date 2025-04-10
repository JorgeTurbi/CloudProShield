using Commons;

namespace Services.Permissions;

public interface IDeleteCommandPermissions
{
  Task<ApiResponse<bool>> Delete(int permissionId);
}
