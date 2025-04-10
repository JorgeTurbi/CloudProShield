using Commons;
using DTOs.Roles;

namespace Services.Roles;

public interface ICreateCommandRoles{

    Task<ApiResponse<bool>> Create(RolesDTO Role);
    
}