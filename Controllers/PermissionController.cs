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
        public PermissionController(IReadCommandPermissions readPermission)
        {
            _readPermission = readPermission;
        }

        [HttpGet("GetAllPermissions")]
        public async Task<IActionResult> GetPermissions()
        {
            ApiResponse<List<PermissionsDTO>> result = await _readPermission.GetAll();
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetPermissionById")]
        public async Task<IActionResult> GetPermissionById(int permissionId)
        {
            ApiResponse<PermissionsDTO> result = await _readPermission.GetById(permissionId);
            if (result.Success == false)
            {
                return BadRequest(new { result });
            }

            return Ok(result);
        }
    }
}