using AutoMapper;
using CloudShield.Entities.Role;
using DTOs.UserRolesPermissions;

namespace CloudShield.Profiles
{
  public class UserRoleWithPermissionsProfile : Profile
  {
    public UserRoleWithPermissionsProfile()
    {
      CreateMap<Role, UserRolePermissionsDTO>()
          .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.Id))
          .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Name))
          .ForMember(dest => dest.RoleDescription, opt => opt.MapFrom(src => src.Description))
          .ForMember(dest => dest.Permissions, opt => opt.Ignore());
    }
  }
}