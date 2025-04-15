using CloudShield.DTOs.UsersDTOs;
using Commons;
using DTOs.Address_DTOS;
using DTOs.Roles;
using DTOs.UsersDTOs;
using Microsoft.AspNetCore.Mvc;
using Services.AddressServices;
using Services.Roles;
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
        private readonly ICreateCommandRoles _createRole;
        private readonly IReadCommandRoles _readRole;
        private readonly IUpdateCommandRoles _updateRole;
        private readonly IDeleteCommandRole _deleteRole;
        private readonly IUserCommandDelete _deleteUser;

        public AccountController(IUserCommandCreate user, IUserCommandRead userRead, IAddress address, IReadCommandRoles readRole, ICreateCommandRoles createRole, IUpdateCommandRoles updateRole, IDeleteCommandRole deleteRole, IUserCommandsUpdate userUpdate, IUserCommandDelete deleteUser)
        {
            _user = user;
            _userRead = userRead;
            _userUpdate = userUpdate;
            _deleteUser = deleteUser;
            _address = address;
            _readRole = readRole;
            _createRole = createRole;
            _updateRole = updateRole;
            _deleteRole = deleteRole;
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

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            var response = await _user.ConfirmEmailAsync(token);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("GetByUserId")]
        public async Task<IActionResult> GetById(int id)
        {
            ApiResponse<AddressDTObyUser> result = await _address.GetAddressbyUserId(id);
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetByEmail")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            ApiResponse<UserDTO_Only> result = await _userRead.GetUserByEmail(email);
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<ApiResponse<string>>> Login([FromBody] UserLoginDTO userLoginDTO)
        {
            if (userLoginDTO == null) return BadRequest("Invalid login request");

            ApiResponse<string> result = await _userRead.LoginUser(userLoginDTO);
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



        [HttpPost("CreateRole")]
        public async Task<ActionResult<ApiResponse<string>>> CreateRole([FromBody] RolesDTO role)
        {
            if (role == null) return BadRequest("Invalid  request");
            return Ok(await _createRole.Create(role));
        }

        [HttpPut("UpdateRole")]
        public async Task<IActionResult> Update([FromBody] RolesDTO roleDTO)
        {
            var result = await _updateRole.Update(roleDTO);

            if (!result.Success)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }


        [HttpGet("GetAllRolesAll")]
        public async Task<IActionResult> GetAllRolesAll()
        {
            var result = await _readRole.GetAll();
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetByRoleId")]
        public async Task<IActionResult> GetRoleById(int RoleId)
        {
            ApiResponse<RolesDTO> result = await _readRole.GetbyId(RoleId);
            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }

        [HttpGet("GetRoleByUserId")]
        public async Task<IActionResult> GetRoleByUserId(int userId)
        {
            var result = await _readRole.GetByUserId(userId);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpDelete("DeleteRole")]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            var result = await _deleteRole.Delete(roleId);

            if (!result.Success)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }
    }
}