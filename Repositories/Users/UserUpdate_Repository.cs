using AutoMapper;
using Commons;
using DataContext;
using DTOs.UsersDTOs;
using Services.UserServices;

namespace Repositories.Users;

public class UserUpdate_Repository : IUserCommandsUpdate
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<UserUpdate_Repository> _log;
  private readonly IMapper _mapper;
  public UserUpdate_Repository(ApplicationDbContext context, ILogger<UserUpdate_Repository> log, IMapper mapper)
  {
    _context = context;
    _log = log;
    _mapper = mapper;
  }

  public async Task<ApiResponse<bool>> Update(UserDTO_Only userDTO)
  {
    try
    {
      if (userDTO == null || userDTO.Id == 0)
      {
        return new ApiResponse<bool>(false, "Invalid user data");
      }

      var user = await _context.User.FindAsync(userDTO.Id);
      if (user == null)
      {
        return new ApiResponse<bool>(false, "user not found");
      }

      _mapper.Map(userDTO, user);

      bool result = await Save();

      _log.LogInformation("user updated successfully");
      return new ApiResponse<bool>(true, "user updated successfully", result);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, ex.Message,"Error occurred while updating user with ID: {Id}", userDTO.Id);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }

public async Task<ApiResponse<bool>> EnableUserAsync(int userId)
{
    try
    {
        var user = await _context.User.FindAsync(userId);
        if (user == null)
        {
            return new ApiResponse<bool>(false, "User not found");
        }

        user.IsActive = true;

        _context.User.Update(user); 

        bool result = await Save();

        if (result)
        {
            _log.LogInformation("User with ID {UserId} was enabled", userId);
            return new ApiResponse<bool>(true, "User enabled successfully", true);
        }

        return new ApiResponse<bool>(false, "Failed to enable user", false);
    }
    catch (Exception ex)
    {
        _log.LogError(ex, "Error enabling user with ID: {UserId}", userId);
        return new ApiResponse<bool>(false, ex.Message, false);
    }
}

public async Task<ApiResponse<bool>>  DisableUserAsync(int userId)
{
    try
    {
        var user = await _context.User.FindAsync(userId);
        if (user == null)
        {
            return new ApiResponse<bool>(false, "User not found");
        }

        user.IsActive = false;

        _context.User.Update(user); // No necesitas await aquí

        bool result = await Save();

        if (result)
        {
            _log.LogInformation("User with ID {UserId} was disabled", userId);
            return new ApiResponse<bool>(true, "User disabled successfully", true);
        }

        return new ApiResponse<bool>(false, "Failed to disabled user", false);
    }
    catch (Exception ex)
    {
        _log.LogError(ex, "Error disabling user with ID: {UserId}", userId);
        return new ApiResponse<bool>(false, ex.Message, false);
    }
}

  private async Task<bool> Save()
  {
    return await _context.SaveChangesAsync() > 0;
  }
}