using AutoMapper;
using CloudShield.DTOs.Country;
using Entities.Users;

namespace CloudShield.Profiles;

public class CountryProfile : Profile
{
  public CountryProfile()
  {
    CreateMap<CountryDTO, Country>().ReverseMap();
  }
}