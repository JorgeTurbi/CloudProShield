using DataContext;
using Microsoft.EntityFrameworkCore;
using Services.Permissions;

namespace Reponsitories.PermissionsValidate_Repository;

public class PermissionsValidate_Repository : IValidatePermissions
{
    private readonly ApplicationDbContext _context;

    private readonly ILogger<PermissionsValidate_Repository> _log;
    public PermissionsValidate_Repository(ApplicationDbContext context, ILogger<PermissionsValidate_Repository> log)
    {
        _context = context;
        _log = log;
    }

    public async Task<bool> Exists(string namePermission)
    {
        try
        {
            string searchPermission = namePermission.ToLower();
            return await _context.Permissions.AsNoTracking().Where(a => a.Name == searchPermission).FirstOrDefaultAsync() != null;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Were an error on validate exists Validate Permission");
            throw;
        }
    }
}