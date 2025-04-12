using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudShield.DTOs.Permissions;

public class RolesPermissionsDTO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }
    public required int UserId { get; set; }
    public required int RoleId { get; set; }
    public required int PermissionsId { get; set; }
}