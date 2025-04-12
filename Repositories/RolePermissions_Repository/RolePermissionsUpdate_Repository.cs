using AutoMapper;
using CloudShield.DTOs.Permissions;
using Commons;
using DataContext;
using Microsoft.EntityFrameworkCore;
using Services.RolePermissions;

namespace Repositories.RolePermissions_Repository;

public class RolePermissionsUpdate_Repository : IUpdateCommandRolePermissions
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<RolePermissionsUpdate_Repository> _log;
  private readonly IMapper _mapper;
  public RolePermissionsUpdate_Repository(ApplicationDbContext context, ILogger<RolePermissionsUpdate_Repository> log, IMapper mapper)
  {
    _context = context;
    _log = log;
    _mapper = mapper;
  }

  public async Task<ApiResponse<bool>> Update(RolesPermissionsDTO rolePermissionDTO)
  {
    try
    {
      var existingRolePermission = await _context.RolePermissions.FirstOrDefaultAsync(rp => rp.Id == rolePermissionDTO.Id);

      if (existingRolePermission == null)
      {
        return new ApiResponse<bool>(false, $"Role permission with ID {rolePermissionDTO.Id} not found", false);
      }

      _mapper.Map(rolePermissionDTO, existingRolePermission);
      bool result = await Save();

      _log.LogInformation("RolePermission updated successfully");
      return new ApiResponse<bool>(true, "RolePermission updated successfully", result);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, ex.Message,"Error occurred while updating role permission with ID: {Id}", rolePermissionDTO.Id);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }

  private async Task<bool> Save()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}