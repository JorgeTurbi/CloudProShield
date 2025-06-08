using CloudShield.Commons;
using CloudShield.Entities.Entity_Address;
using CloudShield.Entities.Operations;
using CloudShield.Entities.Role;

namespace Entities.Users;

public class User : BaseAbstract
{
    public required string Name { get; set; }
    public string SurName { get; set; }
    public DateTime Dob { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Phone { get; set; }
    public required bool IsActive { get; set; }
    public required bool Confirm { get; set; }
    public required string ConfirmToken { get; set; }
    public string ResetPasswordToken { get; set; }
    public DateTime ResetPasswordExpires { get; set; }
    public string Otp { get; set; }
    public DateTime OtpExpires { get; set; }
    public virtual Address Address { get; set; }
    public virtual ICollection<RolePermissions> RolePermissions { get; set; }
    public virtual ICollection<Sessions> Sessions { get; set; }
    public virtual SpaceCloud? SpaceCloud { get; set; }
}
