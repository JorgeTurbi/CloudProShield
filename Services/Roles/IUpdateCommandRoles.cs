using Commons;
using DTOs.Roles;

namespace Services.Roles;

public interface IUpdateCommandRoles
{
      Task<ApiResponse<bool>> Update(RolesDTO Role);
}