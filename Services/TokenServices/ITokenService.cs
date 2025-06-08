
using DTOs.UsersDTOs;

namespace Services.TokenServices;
public interface ITokenService
{
    string GenerateToken(UserDTO user, bool rememberMe);
    string IssueSessionResetToken(Guid userId, string email, string name, TimeSpan life);
    string IssueResetToken(string email, TimeSpan life);
}
