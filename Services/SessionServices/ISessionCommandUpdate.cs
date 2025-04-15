using Commons;

namespace Services.SessionServices
{
    public interface ISessionCommandUpdate
    {
        Task<ApiResponse<bool>> RevokeSession(int sessionId);
        Task<ApiResponse<int>> RevokeAllSessions(int userId);
    }
}