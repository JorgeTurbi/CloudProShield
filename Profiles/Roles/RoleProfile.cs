using AutoMapper;
using CloudShield.Entities.Role;

namespace Roles;

public class RoleProfile:Profile
{
    public RoleProfile()
    {
        CreateMap<RolesDTO, Role>().ReverseMap();
        
    }
}