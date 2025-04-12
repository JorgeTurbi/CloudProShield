using CloudShield.DTOs.Permissions;
using Commons;

namespace Services.RolePermissions
{
    public interface ICreateCommandRolePermissions
    {
        Task<ApiResponse<bool>> Create(RolesPermissionsDTO rolePermission);
    }
}