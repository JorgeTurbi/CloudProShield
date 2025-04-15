using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.UsersDTOs;

public class UserDTO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string SurName { get; set; }
    public DateTime Dob { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Phone { get; set; }
    public string ConfirmToken { get; set; }
    public required int CountryId { get; set; }
    public required int StateId { get; set; }
    public string City { get; set; }
    public string Street { get; set; }
    public string Line { get; set; }
    public required string ZipCode { get; set; }

}