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
            if (listRole.Count > 0)
            {
                List<RolesDTO> found = _mapper.Map<List<RolesDTO>>(listRole);
                return new ApiResponse<List<RolesDTO>>(true, $"Roles Founded {listRole.Count}", found);

            }
            else
            {
                return new ApiResponse<List<RolesDTO>>(false, $"any role found", null);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, ex.Message, "An Error Ocurred on Getting Roles");
            return new ApiResponse<List<RolesDTO>>(false, ex.Message, null);
        }
    }

    public async Task<ApiResponse<RolesDTO>> GetbyId(int RoleId)
    {
        try
        {
            var role = await _context.Role.FirstOrDefaultAsync(r => r.Id == RoleId);

            if (role == null)
            {
                return new ApiResponse<RolesDTO>(false, "Role noy found", null);
            }

            var roleDTO = _mapper.Map<RolesDTO>(role);
            return new ApiResponse<RolesDTO>(true, "Role retrieved successfully", roleDTO);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error retrieving role by ID");
            return new ApiResponse<RolesDTO>(false, "An error ocurred while retrieving the role", null);
        }
    }

    public async Task<ApiResponse<RolesDTO>> GetByUserId(int UserId)
    {
        try
        {
            var roleByUser = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Where(rp => rp.UserId == UserId)
                .ToListAsync();

            if (roleByUser.Count == 0)
            {
                return new ApiResponse<RolesDTO>(false, "No role found for the user", null);
            }

            var roleDTO = _mapper.Map<RolesDTO>(roleByUser);

            return new ApiResponse<RolesDTO>(true, $"Roles found: {roleDTO.Name}", roleDTO);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error retrieving roles for user {UserId}", UserId);
            return new ApiResponse<RolesDTO>(false, "An error occurred while retrieving roles.", null);
        }
    }
}
