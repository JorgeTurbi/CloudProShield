using AutoMapper;
using CloudShield.Entities.Users;
using DTOs;

namespace Profiles;
public class AddressProfile:Profile
{
    public AddressProfile()
    {
        CreateMap<AddressDTOS,Address>().ReverseMap();
    }
}