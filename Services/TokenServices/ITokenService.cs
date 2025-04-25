
using DTOs.UsersDTOs;

namespace Services.TokenServices;
public interface ITokenService
{
    string GenerateToken(UserDTO user, bool rememberMe);
    string IssueSessionResetToken(int userId, string email, string name, TimeSpan life);
    string IssueResetToken(string email, TimeSpan life);
}
