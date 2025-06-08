using CloudShield.DTOs.Permissions;
using Commons;
using Microsoft.AspNetCore.Mvc;
using Services.RolePermissions;

namespace CloudProShield.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolePermissionController : ControllerBase
    {
        private readonly IReadCommandRolePermissions _readRolePermission;
        private readonly ICreateCommandRolePermissions _createRolePermission;
        private readonly IUpdateCommandRolePermissions _updateRolePermission;
        private readonly IDeleteCommandRolePermissions _deleteRolePermission;
        public RolePermissionController(IReadCommandRolePermissions readRolePermission, ICreateCommandRolePermissions createRolePermission, IUpdateCommandRolePermissions updateRolePermission, IDeleteCommandRolePermissions deleteRolePermission)
        {
            _readRolePermission = readRolePermission;
            _createRolePermission = createRolePermission;
            _updateRolePermission = updateRolePermission;
            _deleteRolePermission = deleteRolePermission;
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] RolesPermissionsDTO rolePermission)
        {
            ApiResponse<bool> result = await _createRolePermission.Create(rolePermission);

            if (result.Success == false) return BadRequest(new { result });

            return Created($"api/rolepermission/{rolePermission.Id}", result);
        }

        [HttpGet("GetAllRolePermissions")]
        public async Task<IActionResult> GetRolePermissions()
        {
            ApiResponse<List<RolesPermissionsDTO>> result = await _readRolePermission.GetAll();
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetRolePermissionById")]
        public async Task<IActionResult> GetRolePermissionById(Guid rolePermissionId)
        {
            ApiResponse<RolesPermissionsDTO> result = await _readRolePermission.GetById(rolePermissionId);
            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }

        [HttpGet("GetRolesAndPermissions")]
        public async Task<IActionResult> GetRolesAndPermissions(Guid userId)
        {
            var result = await _readRolePermission.GetRolesAndPermissionsByUserId(userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        [HttpPut("UpdateRolePermission")]
        public async Task<IActionResult> Update([FromBody] RolesPermissionsDTO rolePermissionDTO)
        {
            var result = await _updateRolePermission.Update(rolePermissionDTO);

            if (!result.Success) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpDelete("DeleteRolePermission")]
        public async Task<IActionResult> Delete(int rolePermissionId)
        {
            var result = await _deleteRolePermission.Delete(rolePermissionId);

            if (!result.Success) return BadRequest(new { result });

            return Ok(result);
        }
    }
}