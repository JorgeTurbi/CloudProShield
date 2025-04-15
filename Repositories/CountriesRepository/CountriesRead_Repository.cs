using AutoMapper;
using CloudShield.DTOs.Country;
using Commons;
using DataContext;
using Entities.Users;
using Microsoft.EntityFrameworkCore;
using Services.CountryServices;

namespace Repositories.CountriesRepository;

public class CountriesRead_Repository : IReadCommandCountries
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<CountriesRead_Repository> _log;
  private readonly IMapper _map;
  public CountriesRead_Repository(ApplicationDbContext context, ILogger<CountriesRead_Repository> log, IMapper map)
  {
    _context = context;
    _log = log;
    _map = map;
  }
  public async Task<ApiResponse<List<CountryDTO>>> GetAll()
  {
    try
    {
      List<Country> listCountries = await _context.Country.ToListAsync();

      if (listCountries.Count > 0)
      {
        List<CountryDTO> found = _map.Map<List<CountryDTO>>(listCountries);
        return new ApiResponse<List<CountryDTO>>(true, $"Countries Founded {listCountries.Count}", found);
      }
      else
      {
        return new ApiResponse<List<CountryDTO>>(false, $"any country found", null);
      }
    }
    catch (Exception ex)
    {
      _log.LogError(ex, ex.Message, "An Error Ocurred on Getting RPermissions");
      return new ApiResponse<List<CountryDTO>>(false, ex.Message, null);
    }
  }

  public async Task<ApiResponse<CountryDTO>> GetById(int countryId)
  {
    try
    {
      var country = await _context.Country.FirstOrDefaultAsync(c => c.Id == countryId);

      if (country == null)
      {
        return new ApiResponse<CountryDTO>(false, "Country not found", null);
      }

      var countryDTO = _map.Map<CountryDTO>(country);
      return new ApiResponse<CountryDTO>(true, "Country retrieved successfully", countryDTO);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error retrieving country by ID");
      return new ApiResponse<CountryDTO>(false, "An error ocurred while retrieving the country", null);
    }
  }
}