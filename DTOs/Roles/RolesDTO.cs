using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.Roles;

public class RolesDTO
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}