using CloudShield.Commons;
using Entities.Users;

namespace CloudShield.Entities.Role;

public class RolePermissions : BaseAbstract
{
    public required Guid UserId { get; set; }
    public required Guid RoleId { get; set; }
    public required Guid PermissionsId { get; set; }
    public required virtual User User { get; set; }
    public required virtual Role Role { get; set; }
    public required virtual Permissions Permissions { get; set; }
}