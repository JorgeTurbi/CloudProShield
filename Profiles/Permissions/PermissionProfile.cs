using AutoMapper;
using CloudShield.DTOs.Permissions;
using CloudShield.Entities.Role;

namespace CloudShield.Profiles;

public class PermissionProfile : Profile
{
  public PermissionProfile()
  {
    CreateMap<PermissionsDTO, Permissions>().ReverseMap();
  }
}