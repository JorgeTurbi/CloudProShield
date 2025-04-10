using AutoMapper;
using CloudShield.Entities.Role;
using Commons;
using DataContext;
using DTOs.Roles;
using Microsoft.EntityFrameworkCore;
using Services.Roles;

namespace Repositories.Roles_Repository;

public class RolesRead_Repository : IReadCommandRoles
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RolesRead_Repository> _log;
    private readonly IMapper _mapper;

    public RolesRead_Repository(ApplicationDbContext context, ILogger<RolesRead_Repository> log, IMapper mapper)
    {
        _context = context;
        _log = log;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<RolesDTO>>> GetAll()
    {
        try
        {
            List<Role> listRole = await _context.Role.ToListAsync();
            if (listRole.Count>0)
            {
                List<RolesDTO> found = _mapper.Map<List<RolesDTO>>(listRole);
                return new ApiResponse<List<RolesDTO>>(true,$"Roles Founded {listRole.Count}",found);
                
            }
            else{
                return new ApiResponse<List<RolesDTO>>(false, $"any role found",null);
            }
        }
        catch (Exception ex)
        {
                  _log.LogError(ex,ex.Message,"An Error Ocurred on Create Role");
            return new ApiResponse<List<RolesDTO>>(false,ex.Message,null);
        }
    }

    public Task<ApiResponse<RolesDTO>> GetbyId(int RoleId)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<RolesDTO>> GetByUserId(int UserId)
    {
        throw new NotImplementedException();
    }
}
