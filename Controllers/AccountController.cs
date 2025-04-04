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
    private readonly IUserCommandRead _userRead;

    public AccountController(IUserCommandCreate user, IUserCommandsUpdate userUpdate, IUserCommandRead userRead)
    {
      _user = user;
      _userUpdate = userUpdate;
      _userRead = userRead;
    }

    [HttpPost("Add")]
    public async Task<IActionResult> Add([FromBody] UserCreateUpdateDTO user)
    {
      ApiResponse<bool> result = await _user.AddNew(user);

      if (result.Success == false) return BadRequest(new { result });

      return Created($"api/users/{user.Id}", result);
    }

    [HttpPut("Update")]
    public async Task<IActionResult> Update([FromBody] UserCreateUpdateDTO userDto)
    {
      var result = await _userUpdate.Update(userDto);

      if(!result.Success)
      {
        return BadRequest(new {result});
      }

      return Ok(new { result });
    }

    [HttpGet("GetAllUser")]
    public async Task<IActionResult> GetAllUsers()
    {
      var result = await _userRead.GetAllUsers();

      if (!result.Success) return BadRequest(result);

      return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserById([FromRoute] int id)
    {
      var result = await _userRead.GetUserById(id);

      if (!result.Success) return NotFound(result);

      return Ok(result);
    }
  }
}