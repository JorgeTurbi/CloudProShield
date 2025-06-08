using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.UsersDTOs;

public class UserDTO_Only
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string SurName { get; set; }
    public DateTime Dob { get; set; }
    public required string Email { get; set; }  
    public required string Phone { get; set; }  
}