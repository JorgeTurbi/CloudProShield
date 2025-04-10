namespace Services.Permissions;

public interface IValidatePermissions
{
  Task<bool> Exists(string namePermission);
}