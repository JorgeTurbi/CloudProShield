using AutoMapper;
using CloudShield.DTOs.Permissions;
using CloudShield.Entities.Role;
using Commons;
using DataContext;
using DTOs.UserRolesPermissions;
using Microsoft.EntityFrameworkCore;
using Services.RolePermissions;

namespace Repositories.RolePermissions_Repository;

public class RolePermissionsRead_Repository : IReadCommandRolePermissions
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<RolePermissionsRead_Repository> _log;
  private readonly IMapper _mapper;

  public RolePermissionsRead_Repository(ApplicationDbContext context, ILogger<RolePermissionsRead_Repository> log, IMapper mapper)
  {
    _context = context;
    _log = log;
    _mapper = mapper;
  }
  public async Task<ApiResponse<List<RolesPermissionsDTO>>> GetAll()
  {
    try
    {
      List<RolePermissions> listRolePermissions = await _context.RolePermissions.ToListAsync();

      if (listRolePermissions.Count > 0)
      {
        List<RolesPermissionsDTO> found = _mapper.Map<List<RolesPermissionsDTO>>(listRolePermissions);
        return new ApiResponse<List<RolesPermissionsDTO>>(true, $"RolePermissions Founded {listRolePermissions.Count}", found);
      }
      else
      {
        return new ApiResponse<List<RolesPermissionsDTO>>(false, $"any role permission found", null);
      }
    }
    catch (Exception ex)
    {
      _log.LogError(ex, ex.Message, "An Error Ocurred on Getting Role Permissions");
      return new ApiResponse<List<RolesPermissionsDTO>>(false, ex.Message, null);
    }
  }

  public async Task<ApiResponse<RolesPermissionsDTO>> GetById(int id)
  {
    try
    {
      var rolePermission = await _context.RolePermissions
          .FirstOrDefaultAsync(rp => rp.Id == id);

      if (rolePermission == null)
      {
        return new ApiResponse<RolesPermissionsDTO>(false, $"Role permission with ID {id} not found", null);
      }

      var rolePermissionDto = _mapper.Map<RolesPermissionsDTO>(rolePermission);
      return new ApiResponse<RolesPermissionsDTO>(true, "Role permission found", rolePermissionDto);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, ex.Message, "An Error Occurred on GetById Role Permission");
      return new ApiResponse<RolesPermissionsDTO>(false, ex.Message, null);
    }
  }

  public async Task<ApiResponse<List<UserRolePermissionsDTO>>> GetRolesAndPermissionsByUserId(int userId)
  {
    try
    {
      var rolePerms = await _context.RolePermissions
          .Include(rp => rp.Role)
          .Include(rp => rp.Permissions)
          .Where(rp => rp.UserId == userId)
          .ToListAsync();

      if (!rolePerms.Any())
        return new ApiResponse<List<UserRolePermissionsDTO>>(false, "User has no roles assigned");

      var grouped = rolePerms
          .GroupBy(rp => rp.Role)
          .ToList();

      var dtoList = new List<UserRolePermissionsDTO>();

      foreach (var grp in grouped)
      {
        var dto = _mapper.Map<UserRolePermissionsDTO>(grp.Key);

        dto.Permissions = grp
            .Select(rp => _mapper.Map<PermissionsDTO>(rp.Permissions))
            .DistinctBy(p => p.Id) 
            .ToList();

        dtoList.Add(dto);
      }

      return new ApiResponse<List<UserRolePermissionsDTO>>(true, "Roles and permissions retrieved", dtoList);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error retrieving roles/permissions for user {UserId}", userId);
      return new ApiResponse<List<UserRolePermissionsDTO>>(false, "An error occurred while retrieving data");
    }
  }
}
