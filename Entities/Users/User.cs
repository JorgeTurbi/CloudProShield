using CloudShield.Commons;
using CloudShield.Entities.Role;
using CloudShield.Entities.Users;

namespace Entities.Users;


public class User : BaseAbstract
{

    public required string Name { get; set; }
    public string SurName { get; set; }
    public DateTime Dob { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required bool IsActive { get; set; }
    public required bool Confirm { get; set; }

    public virtual Address Address { get; set; }
    public virtual ICollection<RolePermissions> RolePermissions { get; set; }
    public virtual ICollection<Sessions> Sessions { get; set; }
}