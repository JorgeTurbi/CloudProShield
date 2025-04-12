using AutoMapper;
using CloudShield.DTOs.Permissions;
using CloudShield.Entities.Role;
using Commons;
using DataContext;
using Services.RolePermissions;

namespace Repositories.RolePermissions_Repository;

public class RolePermissionsLib : ICreateCommandRolePermissions
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<RolePermissionsLib> _log;
  private readonly IMapper _mapper;

  public RolePermissionsLib(ApplicationDbContext context, ILogger<RolePermissionsLib> log, IMapper mapper)
  {
    _context = context;
    _log = log;
    _mapper = mapper;
  }

  public async Task<ApiResponse<bool>> Create(RolesPermissionsDTO rolePermission)
  {
    bool result = false;
    try
    {
      result = await Save(rolePermission);
      return new ApiResponse<bool>(
          result,
          result ? $"The role permission with ID {rolePermission.Id} was created successfully" : "Failed to create role permission",
          result);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, ex.Message, "An Error Occurred on Create Role Permission");
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }

  private async Task<bool> Save(RolesPermissionsDTO rolePermission)
  {
    RolePermissions MapRolePermissions = _mapper.Map<RolePermissions>(rolePermission);
    await _context.RolePermissions.AddAsync(MapRolePermissions);
    return await _context.SaveChangesAsync() > 0;
  }
}
