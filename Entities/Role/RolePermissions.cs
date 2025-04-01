
using CloudShield.Commons;
using Entities.Users;



namespace CloudShield.Entities.Role;

public class RolePermissions : BaseAbstract
{
    public required int UserId { get; set; }
    public required int RoleId { get; set; }
    public required int PermissionsId { get; set; }

        public required virtual User User { get; set; }
    public required virtual Role Role { get; set; }
    public required virtual Permissions Permissions { get; set; }
}