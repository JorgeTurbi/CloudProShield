using CloudShield.DTOs.Permissions;
using DTOs.UserRolesPermissions;
using Commons;

namespace Services.RolePermissions
{
    public interface IReadCommandRolePermissions
    {
        Task<ApiResponse<RolesPermissionsDTO>> GetById(int id);
        Task<ApiResponse<List<RolesPermissionsDTO>>> GetAll();
        Task<ApiResponse<List<UserRolePermissionsDTO>>> GetRolesAndPermissionsByUserId(int userId);
    }
}