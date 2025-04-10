using Commons;
using CloudShield.DTOs.Permissions;

namespace Services.Permissions;

public interface IReadCommandPermissions
{
  Task<ApiResponse<List<PermissionsDTO>>> GetAll();
  Task<ApiResponse<PermissionsDTO>> GetById(int PermissionId);
}