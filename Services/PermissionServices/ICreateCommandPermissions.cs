using CloudShield.DTOs.Permissions;
using Commons;

namespace Services.Permissions;

public interface ICreateCommandPermissions
{
  Task<ApiResponse<bool>> Create(PermissionsDTO Permission);
}