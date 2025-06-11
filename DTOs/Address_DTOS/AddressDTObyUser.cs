using System.ComponentModel.DataAnnotations;

namespace DTOs.Address_DTOS;


public class AddressDTObyUser
{
    [Key]
    public Guid Id { get; set; }
    public required string User { get; set; }
    public required string Country { get; set; }
    public required string State { get; set; }
    public required string City { get; set; }
    public string? Street { get; set; }
    public string? Line { get; set; }
    public string? ZipCode { get; set; }
}