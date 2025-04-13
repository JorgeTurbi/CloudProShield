using Commons;
using DTOs.Roles;
using Microsoft.AspNetCore.Mvc;
using Services.Roles;

namespace Controllers;
[ApiController]
[Route("api/[controller]")]
public class RoleController: ControllerBase
{


        private readonly ICreateCommandRoles _createRole;
        private readonly IReadCommandRoles _readRole;
        private readonly IUpdateCommandRoles _updateRole;
        private readonly IDeleteCommandRole _deleteRole;

    public RoleController(IDeleteCommandRole deleteRole, IUpdateCommandRoles updateRole = null, IReadCommandRoles readRole = null, ICreateCommandRoles createRole = null)
    {
        _deleteRole = deleteRole;
        _updateRole = updateRole;
        _readRole = readRole;
        _createRole = createRole;
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