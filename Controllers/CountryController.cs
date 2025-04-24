using CloudShield.DTOs.Country;
using Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.CountryServices;

namespace CloudProShield.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountryController : ControllerBase
    {
        private readonly IReadCommandCountries _readCountry;
        public CountryController(IReadCommandCountries readCountry)
        {
            _readCountry = readCountry;
        }

        [HttpGet("GetAllCountries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCountries()
        {
            ApiResponse<List<CountryDTO>> result = await _readCountry.GetAll();

            if(result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetCountryById")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCountry(int countryId)
        {
            ApiResponse<CountryDTO> result = await _readCountry.GetById(countryId);

            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }
    }
}