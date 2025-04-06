using AutoMapper;
using CloudShield.Entities.Entity_Address;
using DTOs.Address_DTOS;

namespace Profiles.AddressProfiles;


public class AddressProfile: Profile
{

    public AddressProfile()
    {
        CreateMap<AddressDTOS, Address>().ReverseMap();
    }
}