using AutoMapper;
using CloudShield.DTOs.State;
using Entities.Users;

namespace CloudShield.Profiles;

public class StateProfile : Profile
{
  public StateProfile()
  {
    CreateMap<StateDTO, State>().ReverseMap();
  }
}