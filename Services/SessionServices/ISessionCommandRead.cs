using DTOs.Session;
using Commons;

namespace Services.SessionServices
{
    public interface ISessionCommandRead
    {
        Task<ApiResponse<SessionDTO>> GetByToken(string token);
        Task<ApiResponse<SessionDTO>> GetById(Guid sessionId);
        Task<ApiResponse<List<SessionDTO>>> GetByUserId(Guid userId);
        Task<ApiResponse<List<SessionDTO>>> GetAll();
    }
}