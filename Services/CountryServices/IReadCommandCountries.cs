using CloudShield.DTOs.Country;
using Commons;

namespace Services.CountryServices;

public interface IReadCommandCountries
{
  Task <ApiResponse<List<CountryDTO>>> GetAll();
  Task<ApiResponse<CountryDTO>> GetById(int countryId);
}