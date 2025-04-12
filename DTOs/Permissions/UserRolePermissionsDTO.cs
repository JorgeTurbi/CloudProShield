using CloudShield.DTOs.Permissions;

namespace DTOs.UserRolesPermissions
{
    public class UserRolePermissionsDTO
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }

        public List<PermissionsDTO> Permissions { get; set; } = new();
    }
}