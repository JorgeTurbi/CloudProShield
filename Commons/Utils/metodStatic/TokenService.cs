using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DTOs.UsersDTOs;
using Services.TokenServices;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(UserDTO user, bool rememberMe)
    {
        var key = _configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrEmpty(key))
            return null;

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = rememberMe ? DateTime.UtcNow.AddDays(14) : DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string IssueSessionResetToken(int userId, string email, string name, TimeSpan life)
{
    var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.CreateToken(new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name)
        }),
        Expires = DateTime.UtcNow.Add(life),
        SigningCredentials = new(
            new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
    });
    return handler.WriteToken(token);
}

    public string IssueResetToken(string email, TimeSpan life)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, email) }),
            Expires = DateTime.UtcNow.Add(life),
            SigningCredentials = new(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        });
        return handler.WriteToken(token);
    }
}
