using Commons;

namespace Services.RolePermissions;

public interface IDeleteCommandRolePermissions
{
  Task<ApiResponse<bool>> Delete(int id);
}
