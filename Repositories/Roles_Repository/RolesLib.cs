using AutoMapper;
using CloudShield.Entities.Role;
using Commons;
using DataContext;
using DTOs.Roles;
using Services.Roles;

namespace Repositories.Roles_Repository;

public class RolesLib :ICreateCommandRoles
{

        
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<RolesLib> _log;
    private readonly IValidateRoles _validate;

    public RolesLib(ApplicationDbContext context, IMapper mapper, ILogger<RolesLib> log, IValidateRoles validate)
    {
        _context = context;
        _mapper = mapper;
        _log = log;
        _validate = validate;
    }


    public async Task<ApiResponse<bool>> Create(RolesDTO Role)
    {
        bool result=false;
        try
        {
            if (!await _validate.Exists(Role.Name))
            {
                    result = await Save(Role);
            }

            return new ApiResponse<bool>(result,result==true?$"the role {Role.Name} was created succesful": $"An error attempting save/ and a valid Role {Role.Name}",result);
           
        }
        catch (Exception ex)
        {
            _log.LogError(ex,ex.Message,"An Error Ocurred on Create Role");
            return new ApiResponse<bool>(false,ex.Message,false);
        }
    }


    private async Task<bool> Save(RolesDTO role)
    {
        Role MapRole= _mapper.Map<Role>(role);
        await _context.Role.AddAsync(MapRole);
        return await _context.SaveChangesAsync()>0;
    }
}