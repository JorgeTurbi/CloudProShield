using Commons;
using DTOs.UsersDTOs;
using Microsoft.AspNetCore.Mvc;
using Services.UserServices;

namespace CloudShield.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly  IUserCommandCreate _user;
        private readonly IUserCommandRead _userRead;

        public AccountController(IUserCommandCreate user , IUserCommandRead userRead)
        {
            _userRead = userRead;
            _user = user;
        }
     
          [HttpPost("Add")]  
          public async Task<IActionResult> Add([FromBody] UserDTO user)
          {
                    ApiResponse<bool> result = await _user.AddNew(user);

                    if(result.Success==false) return BadRequest(new {result});

                    return Created($"api/users/{user.Id}",result);
          }

            [HttpGet("GetAll")]
            public async Task<IActionResult> GetAll()
            {
                ApiResponse<List<UserDTO_Only>> result = await _userRead.GetAllUsers();
                if(result.Success==false) return BadRequest(new {result});

                return Ok(result);
            }

    }
}