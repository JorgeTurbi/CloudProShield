using AutoMapper;
using DTOs.Session;
using Entities.Users;

namespace CloudProShield.Profiles.Session;

public class SessionProfile : Profile
{
    public SessionProfile()
    {
        CreateMap<SessionDTO, Sessions>().ReverseMap();
    }
}