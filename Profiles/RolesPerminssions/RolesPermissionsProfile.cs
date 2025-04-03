using AutoMapper;
using CloudShield.DTOs.Permissions;
using CloudShield.Entities.Role;

namespace CloudShield.Profiles.RolesPerminssions;

public class RolesPermissionsProfile: Profile
{

    public RolesPermissionsProfile()
    {
        CreateMap<RolesPermissionsDTO, RolePermissions>().ReverseMap();
        
    }
}