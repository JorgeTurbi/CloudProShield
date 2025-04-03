using AutoMapper;
using Commons;
using Commons.Utils;
using DataContext;
using DTOs;
using Entities.Users;
using Microsoft.EntityFrameworkCore;
using Services.UserServices;

namespace CloudShield.Repositories.Users;
public class UserLib : IUserCommandCreate, IUserCommandsUpdate, ISaveServices
{

  private readonly ApplicationDbContext _context;
  private readonly IMapper _mapper;
  private readonly ILogger<UserLib> _log;

  //todo  the user's Contructor
  public UserLib(ApplicationDbContext context, IMapper mapper, ILogger<UserLib> log)
  {
    _context = context;
    _mapper = mapper;
    _log = log;
  }

  //todo Create a new User, it response an object type ApiResponse with boolean data
  public async Task<ApiResponse<bool>> AddNew(UserDTO userDTO, CancellationToken cancellationToken = default)
  {
    //todo validations if user is empty or null

    if (string.IsNullOrEmpty(userDTO.Email))
    {
      //!realizar el log
      return new ApiResponse<bool>(false, "Email Account Invalid");


    }
    //todo validate if exists email account
    var reponse = await Exists(userDTO);
    if (reponse)
    {
        //!realizar el log
      return new ApiResponse<bool>(false, "User Exists");
    }

    //todo mapping before save in database

    var Selected = _mapper.Map<User>(userDTO);

    await _context.User.AddAsync(Selected);
    bool result = await Save();
    _log.LogInformation("Se registro el usuario exitosamente");
    return new ApiResponse<bool>(success: result, message: result == false ? "An Error Ocurred" : "User was Saved");

  }

  //todo to save data
  public async Task<bool> Save(CancellationToken cancellationToken = default)
  {
    try
    {
      return await _context.SaveChangesAsync(cancellationToken) > 0 ? true : false;
    }
    catch (Exception ex)
    {

      _log.LogError(ex, "Error occurred while checking if user exists.");
      throw new Exception("Error occurred while checking if user exists.");
    }
  }

    //todo update user
    public async Task<ApiResponse<bool>> Update(UserDTO userDTO)
    {
        if (userDTO == null || userDTO.Id == 0)
        {
            return new ApiResponse<bool>(false, "Invalid user data");
        }

        var user = await _context.User.FindAsync(userDTO.Id);
        if (user == null)
        {
            return new ApiResponse<bool>(false, "User not found");
        }

        // todo validate email for no repeat
        if (!string.Equals(user.Email, userDTO.Email, StringComparison.OrdinalIgnoreCase))
        {
          bool emailExists = await _context.User.AnyAsync(u => u.Email == userDTO.Email && u.Id != userDTO.Id);
          //todo validate if email exists          
          if (emailExists)
          {
            return new ApiResponse<bool>(false, "Email already exists");
          }
        }

        //todo use AutoMapper to map the properties
        _mapper.Map(userDTO, user);

        //todo update datetime
        user.UpdateAt = DateTime.UtcNow;

        // todo save changes
        bool result = await Save();
        if (result)
        {
            _log.LogInformation("User updated successfully");
            return new ApiResponse<bool>(true, "User updated successfully", result);
        }
        else
        {
            _log.LogError("Error occurred while updating user with ID: {Id}", userDTO.Id);
            return new ApiResponse<bool>(false, "Error occurred while updating user");
        }
    }



    //todo validate if users Exists

    private async Task<bool> Exists(UserDTO userDTO)
  {
    try
    {
      return await _context.User.FirstOrDefaultAsync(a => a.Email == userDTO.Email) != null;
    }
    catch (Exception ex)
    {

      _log.LogError(ex, "Error occurred while checking if user exists.");
      throw new Exception("Error occurred while checking if user exists.");

    }

  }
}