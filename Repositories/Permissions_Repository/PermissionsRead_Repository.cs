using AutoMapper;
using CloudShield.DTOs.Permissions;
using CloudShield.Entities.Role;
using Commons;
using DataContext;
using Microsoft.EntityFrameworkCore;
using Services.Permissions;

namespace Repositories.Permissions_Repository;

public class PermissionsRead_Repository : IReadCommandPermissions
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<PermissionsRead_Repository> _log;
  private readonly IMapper _mapper;

  public PermissionsRead_Repository(ApplicationDbContext context, ILogger<PermissionsRead_Repository> log, IMapper mapper) 
  {
    _context = context;
    _log = log;
    _mapper = mapper;
  }
  public async Task<ApiResponse<List<PermissionsDTO>>> GetAll()
  {
    try
    {
      List<Permissions> listPermissions = await _context.Permissions.ToListAsync();

      if (listPermissions.Count > 0)
      {
        List<PermissionsDTO> found = _mapper.Map<List<PermissionsDTO>>(listPermissions);
        return new ApiResponse<List<PermissionsDTO>>(true, $"Permissions Founded {listPermissions.Count}", found);
      }
      else
      {
        return new ApiResponse<List<PermissionsDTO>>(false, $"any permission found", null);
      }
    }
    catch (Exception ex)
    {
        _log.LogError(ex, ex.Message, "An Error Ocurred on Getting RPermissions");
        return new ApiResponse<List<PermissionsDTO>>(false, ex.Message, null);
    }
  }


  public async Task<ApiResponse<PermissionsDTO>> GetById(Guid PermissionId)
  {
    try
        {
            var permission = await _context.Permissions.FirstOrDefaultAsync(r => r.Id == PermissionId);

            if (permission == null)
            {
                return new ApiResponse<PermissionsDTO>(false, "Permission not found", null);
            }

            var permissionDTO = _mapper.Map<PermissionsDTO>(permission);
            return new ApiResponse<PermissionsDTO>(true, "Permission retrieved successfully", permissionDTO);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error retrieving permission by ID");
            return new ApiResponse<PermissionsDTO>(false, "An error ocurred while retrieving the permission", null);
        }
  }
}