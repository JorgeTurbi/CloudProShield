using Commons;

namespace Services.SessionServices
{
    public interface ISessionCommandUpdate
    {
        Task<ApiResponse<bool>> RevokeSession(string token);
        Task<ApiResponse<int>> RevokeAllSessions(Guid userId);
    }
}