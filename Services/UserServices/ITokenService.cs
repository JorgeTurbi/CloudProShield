using DTOs.UsersDTOs;

namespace Services.UserServices;
public interface ITokenService
{
    string GenerateToken(UserDTO user, bool rememberMe);
}
