

using Commons;

namespace Services.Roles;

public interface IValidateRoles
{

    Task<bool> Exists(string nameRole);
}