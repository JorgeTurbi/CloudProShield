using AutoMapper;
using Commons;
using DataContext;
using Services.Roles;

namespace Repositories.Roles_Repository;

public class RolesDelete_Repository : IDeleteCommandRole
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<RolesDelete_Repository> _log;

  public RolesDelete_Repository(ApplicationDbContext context, ILogger<RolesDelete_Repository> log)
  {
    _context = context;
    _log = log;
  }

  public async Task<ApiResponse<bool>> Delete(int roleId)
  {
    try
    {
      var role = await _context.Role.FindAsync(roleId);

      if (role == null)
      {
        return new ApiResponse<bool>(false, "Role not found.");
      }

      _context.Role.Remove(role);
      bool result = await Save();

      _log.LogInformation("Role {Id} deleted successfully", roleId);
      return new ApiResponse<bool>(true, "Role deleted successfully", result);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error deleting role {Id}", roleId);
      return new ApiResponse<bool>(false, "An error occurred while deleting the role");
    }
  }

  private async Task<bool> Save()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}