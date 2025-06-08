using CloudShield.DTOs.Permissions;
using Commons;
using Microsoft.AspNetCore.Mvc;
using Services.Permissions;

namespace CloudProShield.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly IReadCommandPermissions _readPermission;
        private readonly ICreateCommandPermissions _createPermission;
        private readonly IUpdateCommandPermissions _updatePermission;
        private readonly IDeleteCommandPermissions _deletePermission;
        public PermissionController(IReadCommandPermissions readPermission, ICreateCommandPermissions createPermission, IUpdateCommandPermissions updatePermission, IDeleteCommandPermissions deletePermission)
        {
            _readPermission = readPermission;
            _createPermission = createPermission;
            _updatePermission = updatePermission;
            _deletePermission = deletePermission;
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] PermissionsDTO permission)
        {
            ApiResponse<bool> result = await _createPermission.Create(permission);

            if (result.Success == false) return BadRequest(new { result });

            return Created($"api/permission/{permission.Id}", result);
        }

        [HttpGet("GetAllPermissions")]
        public async Task<IActionResult> GetPermissions()
        {
            ApiResponse<List<PermissionsDTO>> result = await _readPermission.GetAll();
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetPermissionById")]
        public async Task<IActionResult> GetPermissionById(Guid permissionId)
        {
            ApiResponse<PermissionsDTO> result = await _readPermission.GetById(permissionId);
            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }

        [HttpPut("UpdatePermission")]
        public async Task<IActionResult> Update([FromBody] PermissionsDTO permissionDTO)
        {
            var result = await _updatePermission.Update(permissionDTO);

            if (!result.Success)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }

        [HttpDelete("DeleteRole")]
        public async Task<IActionResult> DeletePermission(int permissionId)   
        {
            var result = await _deletePermission.Delete(permissionId);

            if (!result.Success)
            {
                return BadRequest( new { result });
            }

            return Ok(result);
        }
    }
}