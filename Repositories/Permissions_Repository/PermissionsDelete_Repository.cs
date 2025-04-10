using Commons;
using DataContext;
using Services.Permissions;

namespace Repositories.PermissionsDelete_Repository;

public class PermissionsDelete_Repository : IDeleteCommandPermissions
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<PermissionsDelete_Repository> _log;
  public PermissionsDelete_Repository(ApplicationDbContext context, ILogger<PermissionsDelete_Repository> log)
  {
    _context = context;
    _log = log;
  }
  public async Task<ApiResponse<bool>> Delete(int permissionId)
  {
    try
    {
      var permission = await _context.Permissions.FindAsync(permissionId);

      if (permission == null)
      {
        return new ApiResponse<bool>(false, "Permission not found.");
      }

      _context.Permissions.Remove(permission);
      bool result = await Save();

      _log.LogInformation("Permission {Id} deleted successfully", permissionId);
      return new ApiResponse<bool>(true, "Permission deleted successfully", result);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error deleting permission {Id}", permissionId);
      return new ApiResponse<bool>(false, "An error occurred while deleting the permission");
    }
  }

  private async Task<bool> Save()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}