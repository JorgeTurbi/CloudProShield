using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudShield.DTOs.Permissions;

public class RolesPermissionsDTO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required Guid RoleId { get; set; }
    public required Guid PermissionsId { get; set; }
}