using CloudShield.DTOs.UsersDTOs;
using Commons;
using DTOs.Address_DTOS;
using DTOs.UsersDTOs;
using Microsoft.AspNetCore.Mvc;
using Services.AddressServices;
using Services.UserServices;

namespace CloudShield.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IUserCommandCreate _user;
        private readonly IUserCommandRead _userRead;
        private readonly IUserCommandsUpdate _userUpdate;
        private readonly IAddress _address;
        private readonly IUserCommandDelete _deleteUser;

        public AccountController(IUserCommandCreate user, IUserCommandRead userRead, IAddress address, IUserCommandsUpdate userUpdate, IUserCommandDelete deleteUser)
        {
            _user = user;
            _userRead = userRead;
            _userUpdate = userUpdate;
            _deleteUser = deleteUser;
            _address = address;
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] UserDTO user)
        {
            ApiResponse<bool> result = await _user.AddNew(user);

            if (result.Success == false) return BadRequest(new { result });

            return Created($"api/users/{user.Id}", result);
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            ApiResponse<List<UserDTO_Only>> result = await _userRead.GetAllUsers();
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetByUserId")]
        public async Task<IActionResult> GetById(int id)
        {
            ApiResponse<AddressDTObyUser> result = await _address.GetAddressbyUserId(id);
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<ApiResponse<string>>> Login([FromBody] UserLoginDTO userLoginDTO)
        {
            if (userLoginDTO == null) return BadRequest("Invalid login request");

            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            string device = HttpContext.Request.Headers["User-Agent"].ToString() ?? string.Empty;

            ApiResponse<string> result = await _userRead.LoginUser(userLoginDTO, ipAddress, device);
            return Ok(result);
        }

        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UserDTO_Only user)
        {
            var result = await _userUpdate.Update(user);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("EnabledUser")]
        public async Task<IActionResult> EnableUser([FromBody] int user)
        {
            var result = await _userUpdate.EnableUserAsync(user);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        [HttpPut("DisabledUser")]
        public async Task<IActionResult> DisabledUser([FromBody] int user)
        {
            var result = await _userUpdate.DisableUserAsync(user);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _deleteUser.Delete(userId);

            if (!result.Success)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }
    }
}