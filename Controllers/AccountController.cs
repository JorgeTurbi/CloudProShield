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
        private readonly  IUserCommandCreate _user;
        private readonly IUserCommandRead _userRead;
        private readonly IAddress _address;
        private readonly ICreateCommandRoles _createRole;
        private readonly IReadCommandRoles _readRole;


        public AccountController(IUserCommandCreate user, IUserCommandRead userRead, IAddress address, IReadCommandRoles readRole, ICreateCommandRoles createRole)
        {

            _user = user;
            _userRead = userRead;
            _address = address;
            _readRole = readRole;
            _createRole = createRole;
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

            [HttpGet("GetByUserId")]
            public async Task<IActionResult> GetById(int id)
            {
                ApiResponse<AddressDTObyUser> result = await _address.GetAddressbyUserId(id);
                if(result.Success==false) return BadRequest(new {result});

                return Ok(result);
            }


            [HttpGet("GetByEmail")]
            public async Task<IActionResult> GetByEmail(string email)
            {
                ApiResponse<UserDTO_Only> result = await _userRead.GetUserByEmail(email);
                if(result.Success==false) return BadRequest(new {result});

                return Ok(result);
            }

            [HttpPost("Login")]
            public async Task<ActionResult<ApiResponse<string>>> Login([FromBody] UserLoginDTO userLoginDTO)
            {
                  if(userLoginDTO==null) return BadRequest("Invalid login request");
              
               ApiResponse<string> result = await _userRead.LoginUser(userLoginDTO);
                return Ok(result);
            }
                [HttpPost("CreateRole")]
            public async Task<ActionResult<ApiResponse<string>>> Login([FromBody] RolesDTO role)
            {
                  if(role==null) return BadRequest("Invalid  request");
              
              
                return Ok(await _createRole.Create(role));
            }
           
           [HttpGet("GetAllRolesAll")]
            public async Task<IActionResult> GetAllRolesAll()
            {
                
                var result = await _readRole.GetAll();
                if(result.Success==false) return BadRequest(new {result});

                return Ok(result);
            }
       
    }
}