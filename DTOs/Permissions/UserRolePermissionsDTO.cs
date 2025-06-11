using CloudShield.DTOs.Permissions;

namespace DTOs.UserRolesPermissions
{
    public class UserRolePermissionsDTO
    {
        public Guid RoleId { get; set; }
        public required string RoleName { get; set; }
        public string? RoleDescription { get; set; }

        public List<PermissionsDTO> Permissions { get; set; } = new();
    }
}