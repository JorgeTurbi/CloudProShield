
using AutoMapper;
using DTOs;
using Entities.Users;

namespace CloudShield.Profiles;

public class UserProfile : Profile
{
 public UserProfile()
 {
    CreateMap<UserDTO, User>().ReverseMap();
  
 }

}