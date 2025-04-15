using DTOs.Session;
using Commons;

namespace Services.SessionServices
{
    public interface ISessionCommandRead
    {
        Task<ApiResponse<SessionDTO>> GetById(int sessionId);
        Task<ApiResponse<List<SessionDTO>>> GetByUserId(int userId);
        Task<ApiResponse<List<SessionDTO>>> GetAll();
    }
}