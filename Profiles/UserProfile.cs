
using AutoMapper;
using CloudShield.Entities.Users;
using DTOs;
using Entities.Users;
using Users;

namespace CloudShield.Profiles;

public class UserProfile : Profile
{
  public UserProfile()
  {
    // Mapeo para creación/actualización
    CreateMap<UserCreateUpdateDTO, User>()
        .ForMember(dest => dest.Address, opt => opt.Ignore()) // Se mapea por separado
        .ReverseMap();

    // Mapeo de User -> UserCreateUpdateDTO (para editar)
    CreateMap<User, UserCreateUpdateDTO>()
        .ForMember(dest => dest.CountryId, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.CountryId : 0))
        .ForMember(dest => dest.StateId, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.StateId : 0))
        .ForMember(dest => dest.City, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.City : null))
        .ForMember(dest => dest.Street, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.Street : null))
        .ForMember(dest => dest.Line, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.Line : null))
        .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.ZipCode : null));

    // Mapeo para Address desde UserCreateUpdateDTO
    CreateMap<UserCreateUpdateDTO, Address>()
        .ForMember(dest => dest.CountryId, opt => opt.MapFrom(src => src.CountryId))
        .ForMember(dest => dest.StateId, opt => opt.MapFrom(src => src.StateId))
        .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
        .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street))
        .ForMember(dest => dest.Line, opt => opt.MapFrom(src => src.Line))
        .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.ZipCode))
        .ForMember(dest => dest.User, opt => opt.Ignore());

    // Mapeo para listado (GetAllUsers)
    CreateMap<User, UserListDTO>()
        .ForMember(dest => dest.Country, opt => opt.MapFrom(src =>
            src.Address != null && src.Address.Country != null ? src.Address.Country.Name : null))
        .ForMember(dest => dest.State, opt => opt.MapFrom(src =>
            src.Address != null && src.Address.State != null ? src.Address.State.Name : null))
        .ForMember(dest => dest.City, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.City : null));

    // Mapeo para detalle (GetUserById)
    CreateMap<User, UserDetailDTO>()
        .ForMember(dest => dest.CountryId, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.CountryId : 0))
        .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src =>
            src.Address != null && src.Address.Country != null ? src.Address.Country.Name : null))
        .ForMember(dest => dest.StateId, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.StateId : 0))
        .ForMember(dest => dest.StateName, opt => opt.MapFrom(src =>
            src.Address != null && src.Address.State != null ? src.Address.State.Name : null))
        .ForMember(dest => dest.City, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.City : null))
        .ForMember(dest => dest.Street, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.Street : null))
        .ForMember(dest => dest.Line, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.Line : null))
        .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src =>
            src.Address != null ? src.Address.ZipCode : null));
    // CreateMap<UserDTO, User>().ReverseMap();
    // CreateMap<UserListDTO, UserListDTO>().ReverseMap();
    // CreateMap<UserDetailDTO, UserDetailDTO>().ReverseMap();
    // // Map User -> UserDTO (aplanado en Address)
    // CreateMap<User, UserDTO>()
    //             .ForMember(dest => dest.CountryId, opt => opt.MapFrom(src => src.Address.CountryId))
    //             .ForMember(dest => dest.StateId, opt => opt.MapFrom(src => src.Address.StateId))
    //             .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
    //             .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
    //             .ForMember(dest => dest.Line, opt => opt.MapFrom(src => src.Address.Line))
    //             .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.Address.ZipCode))
    //             .ReverseMap()

    //             .ForPath(dest => dest.Address.CountryId, opt => opt.MapFrom(src => src.CountryId))
    //             .ForPath(dest => dest.Address.StateId, opt => opt.MapFrom(src => src.StateId))
    //             .ForPath(dest => dest.Address.City, opt => opt.MapFrom(src => src.City))
    //             .ForPath(dest => dest.Address.Street, opt => opt.MapFrom(src => src.Street))
    //             .ForPath(dest => dest.Address.Line, opt => opt.MapFrom(src => src.Line))
    //             .ForPath(dest => dest.Address.ZipCode, opt => opt.MapFrom(src => src.ZipCode));
  }
}