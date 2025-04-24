using CloudShield.DTOs.State;
using Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.StateServices;

namespace CloudProShield.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StateController : ControllerBase
    {
        private readonly IReadCommandStates _readStates;
        public StateController(IReadCommandStates readStates)
        {
            _readStates = readStates;
        }

        [HttpGet("GetAllStates")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStates()
        {
            ApiResponse<List<StateDTO>> result = await _readStates.GetAll();

            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }

        [HttpGet("GetStateById")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStateById(int stateId)
        {
            ApiResponse<StateDTO> result = await _readStates.GetById(stateId);
            
            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }
    }
}