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
    private readonly IUserCommandCreate _user;
    private readonly IUserCommandsUpdate _userUpdate;

    public AccountController(IUserCommandCreate user, IUserCommandsUpdate userUpdate)
    {
      _user = user;
      _userUpdate = userUpdate;
    }

    [HttpPost("Add")]
    public async Task<IActionResult> Add([FromBody] UserDTO user)
    {
      ApiResponse<bool> result = await _user.AddNew(user);

      if (result.Success == false) return BadRequest(new { result });

      return Created($"api/users/{user.Id}", result);
    }

    [HttpPut("Update")]
    public async Task<IActionResult> Update([FromBody] UserDTO userDto)
    {
      var result = await _userUpdate.Update(userDto);

      if(!result.Success)
      {
        return BadRequest(new {result});
      }

      return Ok(new { result });
    }
  }
}