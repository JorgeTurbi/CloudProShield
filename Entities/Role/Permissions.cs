using CloudShield.Commons;

namespace CloudShield.Entities.Role;

public class Permissions:BaseAbstract
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public virtual  ICollection<RolePermissions>? RolePermissions { get; set; }
}