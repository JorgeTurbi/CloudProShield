using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CloudShield.Entities.Entity_Address;

namespace Entities.Users;

public class State
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }
    public int CountryId { get; set; }
    public string Name { get; set; }
    public virtual Country Country { get; set; }
    public ICollection<Address> Address { get; set; } = new List<Address>();
}