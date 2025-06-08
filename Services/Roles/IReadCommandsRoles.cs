using Commons;
using DTOs.Roles;

namespace Services.Roles;

public interface IReadCommandRoles
{
    Task<ApiResponse<List<RolesDTO>>> GetAll();
    Task<ApiResponse<RolesDTO>> GetbyId(Guid RoleId);
    Task<ApiResponse<RolesDTO>> GetByUserId(Guid UserId);
}