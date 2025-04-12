using CloudShield.DTOs.Permissions;
using Commons;

namespace Services.RolePermissions
{
    public interface IReadCommandRolePermissions
    {
        Task<ApiResponse<RolesPermissionsDTO>> GetById(int id);
        Task<ApiResponse<List<RolesPermissionsDTO>>> GetAll();
    }
}