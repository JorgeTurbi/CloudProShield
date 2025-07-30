namespace DTOs.UsersDTOs;

public class UserAutoCreateDTO
{
    public required Guid Id { get; set; } // GUID del AuthService
    public required string Name { get; set; }
    public required string SurName { get; set; }
    public DateTime? Dob { get; set; } // Opcional para companies
    public required string Email { get; set; }
    public required string PlainPassword { get; set; } // Contraseña en texto plano para hashear
    public string Phone { get; set; } = string.Empty;
    public bool IsCompany { get; set; }
    public string? CompanyName { get; set; }
    public string? FullName { get; set; }
    public string? Domain { get; set; }

    // Dirección predeterminada - valores válidos para República Dominicana
    public int CountryId { get; set; } = 220; // República Dominicana
    public int StateId { get; set; } = 1; // Estado por defecto
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? Line { get; set; }
    public string? ZipCode { get; set; }
    public string? Plan { get; set; }
}
