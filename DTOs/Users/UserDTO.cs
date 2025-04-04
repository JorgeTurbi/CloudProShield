using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace DTOs;

public class UserDTO
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Key]
  public int Id { get; set; }
  public required string Name { get; set; }
  public string SurName { get; set; }
  public DateTime Dob { get; set; }
  public required string Email { get; set; }
  public required string Password { get; set; }
  public required string Phone { get; set; }
  public int CountryId { get; set; }
  public int StateId { get; set; }
  public string City { get; set; }
  public string Street { get; set; }
  public string Line { get; set; }
  public string ZipCode { get; set; }

}