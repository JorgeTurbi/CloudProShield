using AutoMapper;
using Commons;
using DataContext;
using Microsoft.EntityFrameworkCore;
using Services.Roles;

namespace Reponsitories.Roles_Repository;

public class RolesValidate_Repository : IValidateRoles
{
        private readonly ApplicationDbContext _context;

    private readonly ILogger<RolesValidate_Repository> _log;
    private readonly IMapper _mapper;
    public RolesValidate_Repository(ApplicationDbContext context, ILogger<RolesValidate_Repository> log, IMapper mapper)
    {
        _context = context;
        _log = log;
        _mapper = mapper;
    }


    public async  Task<bool> Exists(string nameRole)
    {
            try
            {
                string searchRole = nameRole.ToLower();
              return await _context.Role.AsNoTracking().Where(a=>a.Name==searchRole).FirstOrDefaultAsync()!=null;  
                
                
            }
            catch (Exception ex)
            {
                _log.LogError(ex,"Were an error on validate exists Validate Role");
               throw;
            }
    }
}