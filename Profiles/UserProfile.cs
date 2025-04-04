
using AutoMapper;
using DTOs;
using Entities.Users;
using Users;

namespace CloudShield.Profiles;

public class UserProfile : Profile
{
  public UserProfile()
  {
    CreateMap<UserDTO, User>().ReverseMap();
    CreateMap<UserListDTO, UserListDTO>().ReverseMap();
    // Map User -> UserDTO (aplanado en Address)
    CreateMap<User, UserDTO>()
                .ForMember(dest => dest.CountryId, opt => opt.MapFrom(src => src.Address.CountryId))
                .ForMember(dest => dest.StateId, opt => opt.MapFrom(src => src.Address.StateId))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
                .ForMember(dest => dest.Line, opt => opt.MapFrom(src => src.Address.Line))
                .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.Address.ZipCode))
                .ReverseMap()
                
                .ForPath(dest => dest.Address.CountryId, opt => opt.MapFrom(src => src.CountryId))
                .ForPath(dest => dest.Address.StateId, opt => opt.MapFrom(src => src.StateId))
                .ForPath(dest => dest.Address.City, opt => opt.MapFrom(src => src.City))
                .ForPath(dest => dest.Address.Street, opt => opt.MapFrom(src => src.Street))
                .ForPath(dest => dest.Address.Line, opt => opt.MapFrom(src => src.Line))
                .ForPath(dest => dest.Address.ZipCode, opt => opt.MapFrom(src => src.ZipCode));
  }
}