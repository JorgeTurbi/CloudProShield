
using DTOs.UsersDTOs;

namespace Services.TokenServices;
public interface ITokenService
{
    string GenerateToken(UserDTO user, bool rememberMe);
    string IssueResetToken(string email, TimeSpan life);
 
}
