using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CloudShield.DTOs.Permissions;

public class PermissionsDTO
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Key]
  public int Id { get; set; }
  public string Name { get; set; }
  public string Description { get; set; }
}