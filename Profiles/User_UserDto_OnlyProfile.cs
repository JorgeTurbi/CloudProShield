using AutoMapper;
using DTOs.UsersDTOs;
using Entities.Users;

namespace Profiles;

public class User_UserDto_OnlyProfile : Profile
{
    public User_UserDto_OnlyProfile()
    {
            CreateMap<UserDTO_Only, User>().ReverseMap();
    }
}
