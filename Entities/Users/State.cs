using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http.Headers;
using CloudShield.Entities.Users;

namespace Entities.Users;

public class State
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }
    public int CountryId { get; set; }
    public string Name { get; set; }
    public virtual Country Country {get;set;}
    public virtual Address Address { get; set; }
}