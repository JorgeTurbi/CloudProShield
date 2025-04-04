using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Users;
public class UserListDTO 
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }
    public  string Name { get; set; }
    public string SurName { get; set; }
    public DateTime Dob { get; set; }
    public  string Email { get; set; }
    public  string Password { get; set; }
    public  string Phone { get; set; } 
      public string Country { get; set; }
      public string State { get; set; }
      public string City { get; set; }
      public string Street { get; set; }
      public string Line { get; set; }
      public string ZipCode { get; set; }
}