using Commons;
using DTOs.Session;

namespace Services.SessionServices
{
  public interface ISessionCommandCreate
  {
    Task<ApiResponse<SessionDTO>> CreateSession(Guid userId, string ip, string device);
  }
}