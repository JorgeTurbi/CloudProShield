using DataContext;
using Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace CloudShield.Repositories.Users;

public class UserPassword_Repository
{
  private readonly ApplicationDbContext _context;
  public UserPassword_Repository(ApplicationDbContext context)
  {
    _context = context;
  }

  public Task<User> FindByEmailAsync(string email)
  {
    return _context.User.FirstOrDefaultAsync(u => u.Email == email);
  }

  public Task<User> FindForResetAsync(string email, string token)
  {
    return _context.User
      .FirstOrDefaultAsync(u => u.Email == email && u.ResetPasswordToken == token && u.ResetPasswordExpires > DateTime.UtcNow);
  }

  public async Task SaveAsync()
  {
    await _context.SaveChangesAsync();
  }
}