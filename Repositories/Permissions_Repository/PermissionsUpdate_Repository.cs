using AutoMapper;
using CloudShield.DTOs.Permissions;
using Commons;
using DataContext;
using Services.Permissions;

namespace Repositories.PermissionsUpdate_Repository;

public class PermissionsUpdate_Repository : IUpdateCommandPermissions
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<PermissionsUpdate_Repository> _log;
  private readonly IMapper _mapper;
  public PermissionsUpdate_Repository(ApplicationDbContext context, ILogger<PermissionsUpdate_Repository> log, IMapper mapper)
  {
    _context = context;
    _log = log;
    _mapper = mapper;
  }
  public async Task<ApiResponse<bool>> Update(PermissionsDTO permissionDTO)
  {
    try
    {
      if (permissionDTO == null || permissionDTO.Id == Guid.Empty || string.IsNullOrWhiteSpace(permissionDTO.Name))
      {
        return new ApiResponse<bool>(false, "Invalid permission data");
      }

      var permission = await _context.Permissions.FindAsync(permissionDTO.Id);
      if (permission == null)
      {
        return new ApiResponse<bool>(false, "Permission not found." );
      }

      _mapper.Map(permissionDTO, permission);
      bool result = await Save();

      _log.LogInformation("Permission updated successfully");
      return new ApiResponse<bool>(true, "Role updated successfully", result);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, ex.Message,"Error occurred while updating permission with ID: {Id}", permissionDTO.Id);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }

  private async Task<bool> Save()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}