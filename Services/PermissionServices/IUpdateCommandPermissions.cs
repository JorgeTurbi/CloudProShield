using CloudShield.DTOs.Permissions;
using Commons;

namespace Services.Permissions;

public interface IUpdateCommandPermissions
{
  Task<ApiResponse<bool>> Update(PermissionsDTO permissionDTO);
}
