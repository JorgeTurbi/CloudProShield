using AutoMapper;
using Commons;
using DataContext;
using DTOs.UsersDTOs;
using Services.UserServices;

namespace Repositories.Users;

public class UserDelete_Repository : IUserCommandDelete
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<UserDelete_Repository> _log;
  private readonly IMapper _mapper;
  public UserDelete_Repository(ApplicationDbContext context, ILogger<UserDelete_Repository> log, IMapper mapper)
  {
    _context = context;
    _log = log;
    _mapper = mapper;
  }


  public async Task<ApiResponse<bool>> Delete(int userId)
  {
    try
    {
      var user = await _context.User.FindAsync(userId);

      if (user == null)
      {
        return new ApiResponse<bool>(false, "User not found.");
      }

      _context.User.Remove(user);
      bool result = await Save();

      _log.LogInformation("User {Id} deleted successfully", userId);
      return new ApiResponse<bool>(true, "User deleted successfully", result);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error deleting User {Id}", userId);
      return new ApiResponse<bool>(false, "An error occurred while deleting the User");
    }
  }

 private async Task<bool> Save()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}