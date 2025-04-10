using Commons;


namespace Services.Roles;

public interface IDeleteCommandRole
{
    Task<ApiResponse<bool>> Delete(int RoleId);
}