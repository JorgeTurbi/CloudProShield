using AutoMapper;
using DTOs.Address_DTOS;
using DTOs.UsersDTOs;

namespace Profiles;

public class AddressDTOProfile: Profile
{
    public AddressDTOProfile()
    {
        CreateMap<UserDTO, AddressDTOS>().ReverseMap();
    }
}