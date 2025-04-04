using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs;

public class UserCreateUpdateDTO
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Key]
  public int Id { get; set; }
  public string Name { get; set; }
  public string SurName { get; set; }
  [DataType(DataType.Date)]
  public DateTime Dob { get; set; }
  public string Email { get; set; }
  public string Password { get; set; }
  public string Phone { get; set; }

  // Datos de dirección
  public int CountryId { get; set; }
  public int StateId { get; set; }
  public string City { get; set; }
  public string Street { get; set; }
  public string Line { get; set; }
  public string ZipCode { get; set; }
}