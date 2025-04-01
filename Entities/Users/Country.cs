using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CloudShield.Entities.Users;

namespace Entities.Users;

public class Country 
{
[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
[Key]
public int Id { get; set; }
public string Name { get; set; }

public virtual ICollection<State> State {get;set;}
public virtual Address Address { get; set; }

}