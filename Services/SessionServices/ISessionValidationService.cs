using DTOs.Session;

namespace Services.SessionServices;

public interface ISessionValidationService
{
  Task<SessionDTO> ValidateTokenAsync(string token);
}