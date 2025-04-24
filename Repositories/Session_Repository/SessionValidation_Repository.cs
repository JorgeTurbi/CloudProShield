using DTOs.Session;
using Services.SessionServices;

namespace Repositories.Session_Repository;

public class SessionValidation_Repository : ISessionValidationService
{
    private readonly ISessionCommandRead _sessionRead;

    public SessionValidation_Repository(ISessionCommandRead sessionRead)
    {
        _sessionRead = sessionRead;
    }

    public async Task<SessionDTO> ValidateTokenAsync(string token)
    {
        var res = await _sessionRead.GetByToken(token);
        return res.Success ? res.Data : null;
    }
}