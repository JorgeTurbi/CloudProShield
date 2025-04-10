using AutoMapper;
using CloudShield.DTOs.Permissions;
using CloudShield.Entities.Role;
using Commons;
using DataContext;
using Services.Permissions;

namespace Repositories.Permissions_Repository;

public class PermissionsLib : ICreateCommandPermissions
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<PermissionsLib> _log;
  private readonly IMapper _mapper;
  private readonly IValidatePermissions _validate;
  public PermissionsLib(ApplicationDbContext context, ILogger<PermissionsLib> log, IMapper mapper, IValidatePermissions validate)
  {
    _context = context;
    _log = log;
    _mapper = mapper;
    _validate = validate;
  }
  public async Task<ApiResponse<bool>> Create(PermissionsDTO Permission)
  {
    bool result = false;
    try
    {
      if (!await _validate.Exists(Permission.Name))
      {
        result = await Save(Permission);
      }

      return new ApiResponse<bool>(result, result == true ? $"the permisssion {Permission.Name} was created succesful" : $"An error attempting save/ and a valid Permission {Permission.Name}", result);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, ex.Message, "An Error Ocurred on Create Permission");
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }

  private async Task<bool> Save(PermissionsDTO permission)
  {
    Permissions MapPermissions = _mapper.Map<Permissions>(permission);
    await _context.Permissions.AddAsync(MapPermissions);
    return await _context.SaveChangesAsync() > 0;
  }
}