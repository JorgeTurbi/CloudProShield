using AutoMapper;
using CloudShield.Entities.Role;
using DTOs.Roles;

namespace Roles;

public class RoleProfile:Profile
{
    public RoleProfile()
    {
        CreateMap<RolesDTO, Role>().ReverseMap();   
    }
}