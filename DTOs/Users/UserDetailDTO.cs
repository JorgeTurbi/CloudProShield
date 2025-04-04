using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Users;

public class UserDetailDTO
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Key]
  public int Id { get; set; }
  public string Name { get; set; }
  public string SurName { get; set; }
  public DateTime Dob { get; set; }
  public string Email { get; set; }
  public string Phone { get; set; }
  public int CountryId { get; set; }
  public string CountryName { get; set; }
  public int StateId { get; set; }
  public string StateName { get; set; }
  public string City { get; set; }
  public string Street { get; set; }
  public string Line { get; set; }
  public string ZipCode { get; set; }
}