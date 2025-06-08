using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.Address_DTOS;


public class AddressDTOS
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int CountryId { get; set; }
    public int StateId { get; set; }
    public string City { get; set; }
    public string Street { get; set; }
    public string Line { get; set; }
    public string ZipCode { get; set; }
}