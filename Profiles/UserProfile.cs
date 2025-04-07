
using AutoMapper;
using DTOs.UsersDTOs;
using Entities.Users;

namespace CloudShield.Profiles;

public class UserProfile : Profile
{
 public UserProfile()
 {
    CreateMap<UserDTO, User>().ReverseMap();
  
 }

}