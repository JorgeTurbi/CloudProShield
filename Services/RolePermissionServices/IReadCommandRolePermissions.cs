using CloudShield.DTOs.Permissions;
using Commons;

namespace Services.RolePermissions
{
    public interface IReadCommandRolePermissions
    {
        Task<ApiResponse<RolesPermissionsDTO>> GetById(int id);
        Task<ApiResponse<List<RolesPermissionsDTO>>> GetAll();
        Task<ApiResponse<List<RolesPermissionsDTO>>> GetByRoleId(int roleId);
        Task<ApiResponse<List<RolesPermissionsDTO>>> GetByPermissionId(int permissionId);
    }
}