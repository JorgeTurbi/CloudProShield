using Commons;
using DTOs.UsersDTOs;
using Microsoft.AspNetCore.Mvc;
using Services.Roles;
using Services.UserServices;

namespace Controllers;

 [ApiController]
    [Route("api/[controller]")]
public class EmailController: ControllerBase
{
            private readonly IReadCommandRoles _readRole;
                    private readonly IUserCommandCreate _user;
                     private readonly IUserCommandRead _userRead;
    public EmailController(IReadCommandRoles readRole, IUserCommandCreate user, IUserCommandRead userRead)
    {
        _readRole = readRole;
        _user = user;
        _userRead = userRead;
    }



    [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            var response = await _user.ConfirmEmailAsync(token);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

          [HttpGet("GetByEmail")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            ApiResponse<UserDTO_Only> result = await _userRead.GetUserByEmail(email);
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }


}