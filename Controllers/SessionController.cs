using System.Security.Claims;
using Commons;
using DTOs.Session;
using Microsoft.AspNetCore.Mvc;
using Services.SessionServices;

namespace CloudProShield.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionController : ControllerBase
    {
        private readonly ISessionCommandRead _sessionRead;
        private readonly ISessionCommandUpdate _sessionUpdate;

        public SessionController(ISessionCommandRead sessionRead, ISessionCommandUpdate sessionUpdate)
        {
            _sessionRead = sessionRead;
            _sessionUpdate = sessionUpdate;
        }

        [HttpPut("LogoutAll")]
        public async Task<IActionResult> LogoutAll(Guid userId)
        {
            ApiResponse<int> result = await _sessionUpdate.RevokeAllSessions(userId);

            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }

        [HttpPut("Logout")]
        public async Task<IActionResult> Logout([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { success = false, message = "Token is required" });
            }
            
            ApiResponse<bool> result = await _sessionUpdate.RevokeSession(token);

            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }

        [HttpGet("GetAllSessions")]
        public async Task<IActionResult> GetAllSessions()
        {
            ApiResponse<List<SessionDTO>> result = await _sessionRead.GetAll();

            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }

        [HttpGet("GetSessionById")]
        public async Task<IActionResult> GetSessionById(Guid sessionId)
        {
            ApiResponse<SessionDTO> result = await _sessionRead.GetById(sessionId);

            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }

        [HttpGet("GetSessionByUserId")]
        public async Task<IActionResult> GetSessionByUserId(Guid userId)
        {
            ApiResponse<List<SessionDTO>> result = await _sessionRead.GetByUserId(userId);

            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }
    }
}