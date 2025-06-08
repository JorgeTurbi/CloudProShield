using AutoMapper;
using Commons;
using DataContext;
using DTOs.Roles;
using Services.Roles;

namespace Repositories.RoleUpdate_Repository;

public class RoleUpdate_Repository : IUpdateCommandRoles
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<RoleUpdate_Repository> _log;
  private readonly IMapper _mapper;
  public RoleUpdate_Repository(ApplicationDbContext context, ILogger<RoleUpdate_Repository> log, IMapper mapper)
  {
    _context = context;
    _log = log;
    _mapper = mapper;
  }

  public async Task<ApiResponse<bool>> Update(RolesDTO roleDTO)
  {
    try
    {
      if (roleDTO == null || roleDTO.Id == Guid.Empty || string.IsNullOrWhiteSpace(roleDTO.Name))
      {
        return new ApiResponse<bool>(false, "Invalid role data");
      }

      var role = await _context.Role.FindAsync(roleDTO.Id);
      if (role == null)
      {
        return new ApiResponse<bool>(false, "Role not found");
      }

      _mapper.Map(roleDTO, role);

      bool result = await Save();

      _log.LogInformation("Role updated successfully");
      return new ApiResponse<bool>(true, "Role updated successfully", result);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, ex.Message,"Error occurred while updating role with ID: {Id}", roleDTO.Id);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }

  private async Task<bool> Save()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}