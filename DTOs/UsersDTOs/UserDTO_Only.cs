using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.UsersDTOs;
/// <summary>
/// UserDTO class represents a data transfer object for user information.
/// It contains properties for user details such as name, surname, date of birth, email, password, phone number, and address information.
/// </summary>
//

public class UserDTO_Only
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string SurName { get; set; }
    public DateTime Dob { get; set; }
    public required string Email { get; set; }  
    public required string Phone { get; set; }  
}