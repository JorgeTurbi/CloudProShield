using Commons;
using DataContext;
using Services.RolePermissions;

namespace Repositories.RolePermissions_Repository;

public class RolePermissionsDelete_Repository : IDeleteCommandRolePermissions
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<RolePermissionsDelete_Repository> _log;
  public RolePermissionsDelete_Repository(ApplicationDbContext context, ILogger<RolePermissionsDelete_Repository> log)
  {
    _context = context;
    _log = log;
  }
  public async Task<ApiResponse<bool>> Delete(int id)
  {
    try
    {
      var rolePermission = await _context.RolePermissions.FindAsync(id);

      if (rolePermission == null)
      {
        return new ApiResponse<bool>(false, "Role permission not found.");
      }

      _context.RolePermissions.Remove(rolePermission);
      bool result = await Save();

      _log.LogInformation("Role permission {Id} deleted successfully", id);
      return new ApiResponse<bool>(true, "Role permission deleted successfully", result);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error deleting role permission {Id}", id);
      return new ApiResponse<bool>(false, "An error occurred while deleting the role permission");
    }
  }

  private async Task<bool> Save()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}