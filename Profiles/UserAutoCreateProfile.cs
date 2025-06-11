using AutoMapper;
using DTOs.UsersDTOs;
using Entities.Users;

namespace Profiles;

public class UserAutoCreateProfile : Profile
{
    public UserAutoCreateProfile()
    {
        CreateMap<UserAutoCreateDTO, User>()
            .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Confirm, opt => opt.MapFrom(src => true));
    }
}
