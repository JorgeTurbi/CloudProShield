using CloudShield.DTOs.Permissions;
using Commons;

namespace Services.RolePermissions
{
    public interface IUpdateCommandRolePermissions
    {
        Task<ApiResponse<bool>> Update(RolesPermissionsDTO rolePermission);
    }
}