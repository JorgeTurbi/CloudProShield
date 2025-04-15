using Commons;
using DTOs.Session;

namespace Services.SessionServices
{
  public interface ISessionCommandCreate
  {
    Task<ApiResponse<SessionDTO>> CreateSession(int userId, string ip, string device);
  }
}