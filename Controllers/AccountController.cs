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
        private readonly  IUserCommandCreate _user;
        private readonly IUserCommandRead _userRead;
        private readonly IAddress _address;
      

        public AccountController(IUserCommandCreate user , IUserCommandRead userRead , IAddress address)
        {
           
            _user = user;
            _userRead = userRead;
            _address = address;
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

            [HttpGet("GetByUserId/{id}")]
            public async Task<IActionResult> GetById(int id)
            {
                ApiResponse<AddressDTObyUser> result = await _address.GetAddressbyUserId(id);
                if(result.Success==false) return BadRequest(new {result});

                return Ok(result);
            }


            [HttpGet("GetByEmail/{email}")]
            public async Task<IActionResult> GetByEmail(string email)
            {
                ApiResponse<UserDTO_Only> result = await _userRead.GetUserByEmail(email);
                if(result.Success==false) return BadRequest(new {result});

                return Ok(result);
            }
    }
}