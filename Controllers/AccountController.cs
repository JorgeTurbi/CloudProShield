using Commons;
using DTOs;

using Microsoft.AspNetCore.Mvc;
using Services.UserServices;

namespace CloudShield.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly  IUserCommandCreate _user;

        public AccountController(IUserCommandCreate user)
        {
            _user = user;
        }

          [HttpPost("Add")]  
          public async Task<IActionResult> Add([FromBody] UserDTO user)
          {
                    ApiResponse<bool> result = await _user.AddNew(user);

                    if(result.Success==false) return BadRequest(new {result});

                    return Created($"api/users/{user.Id}",result);
          }

    }
}