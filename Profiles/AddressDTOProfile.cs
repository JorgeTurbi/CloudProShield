using AutoMapper;
using DTOs;

namespace Profiles;

public class AddressDTOProfile: Profile
{
    public AddressDTOProfile()
    {
        CreateMap<UserDTO, AddressDTOS>().ReverseMap();
    }
}