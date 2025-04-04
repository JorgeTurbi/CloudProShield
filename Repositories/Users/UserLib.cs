using AutoMapper;
using CloudShield.Entities.Users;
using Commons;
using Commons.Utils;
using DataContext;
using DTOs;
using Entities.Users;
using Microsoft.EntityFrameworkCore;
using Services.UserServices;
using Users;

namespace CloudShield.Repositories.Users;
public class UserLib : IUserCommandCreate, IUserCommandRead, IUserCommandsUpdate, ISaveServices
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
  public async Task<ApiResponse<bool>> AddNew(UserCreateUpdateDTO userDTO, CancellationToken cancellationToken = default)
  {
    //todo validations if user is empty or null
    if (string.IsNullOrEmpty(userDTO.Email))
    {
      //!realizar el log
      return new ApiResponse<bool>(false, "Email Account Invalid");
    }

    //todo validate if exists email account
    var reponse = await Exists(userDTO.Email);
    if (reponse)
    {
      //!realizar el log
      return new ApiResponse<bool>(false, "User Exists");
    }

    //todo mapping before save in database
    var user = _mapper.Map<User>(userDTO);
    var address = _mapper.Map<Address>(userDTO);
    user.Address = address;

    await _context.User.AddAsync(user, cancellationToken);
    bool result = await Save();
    _log.LogInformation("User registered successfully: {Email}", userDTO.Email);
    return new ApiResponse<bool>(success: result, 
        message: result ? "User was saved successfully" : "An error occurred");
  }

  //todo get all users
  public async Task<ApiResponse<List<UserListDTO>>> GetAllUsers()
  {
    try
    {
      var users = await _context.User
            .Include(u => u.Address)
                .ThenInclude(a => a.Country)
            .Include(u => u.Address)
                .ThenInclude(a => a.State)
            .Select(u => new UserListDTO
            {
                Id = u.Id,
                Name = u.Name,
                SurName = u.SurName,
                Email = u.Email,
                Phone = u.Phone,
                Dob = u.Dob,
                Street = u.Address != null ? u.Address.Street : null,
                Line = u.Address != null ? u.Address.Line : null,
                ZipCode = u.Address != null ? u.Address.ZipCode : null,
                Country = u.Address != null && u.Address.Country != null ? u.Address.Country.Name : null,
                State = u.Address != null && u.Address.State != null ? u.Address.State.Name : null,
                City = u.Address != null ? u.Address.City : null
            })
            .ToListAsync();
        
      _log.LogInformation("Retrieved {Count} users from the database.", users.Count);
      return new ApiResponse<List<UserListDTO>>(true, "Users retrieved successfully", data: users);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error occurred while retrieving All users.");
      return new ApiResponse<List<UserListDTO>>(false, "Error occurred while retrieving users.");
    }
  }

  //todo get user by id
  public async Task<ApiResponse<UserDetailDTO>> GetUserById(int id)
  {
    try
    {
      var user = await _context.User
            .Include(u => u.Address)
                .ThenInclude(a => a.Country)
            .Include(u => u.Address)
                .ThenInclude(a => a.State)
            .Where(u => u.Id == id)
            .Select(u => new UserDetailDTO
            {
                Id = u.Id,
                Name = u.Name,
                SurName = u.SurName,
                Email = u.Email,
                Phone = u.Phone,
                CountryId = u.Address != null ? u.Address.CountryId : 0,
                CountryName = u.Address != null && u.Address.Country != null ? u.Address.Country.Name : null,
                StateId = u.Address != null ? u.Address.StateId : 0,
                StateName = u.Address != null && u.Address.State != null ? u.Address.State.Name : null,
                City = u.Address != null ? u.Address.City : null,
                Street = u.Address != null ? u.Address.Street : null,
                Line = u.Address != null ? u.Address.Line : null,
                ZipCode = u.Address != null ? u.Address.ZipCode : null
            })
            .FirstOrDefaultAsync();

      if (user == null)
      {
        _log.LogWarning("User not found for ID: {Id}", id);
        return new ApiResponse<UserDetailDTO>(false, "User not found");
      }

      _log.LogInformation("User with ID {Id} retrieved successfully.", id);
      return new ApiResponse<UserDetailDTO>(true, "User retrieved successfully", data: user);
    }
    catch (Exception Exception)
    {
      _log.LogError(Exception, "Error occurred while retrieving user with ID: {Id}", id);
      return new ApiResponse<UserDetailDTO>(false, "Error occurred while retrieving user");
    }
  }

  //todo to save data
  public async Task<bool> Save(CancellationToken cancellationToken = default)
  {
    try
    {
      return await _context.SaveChangesAsync(cancellationToken) > 0;
    }
    catch (Exception ex)
    {

      _log.LogError(ex, "Error occurred while checking if user exists.");
      throw new Exception("Error occurred while checking if user exists.");
    }
  }

  //todo update user
  public async Task<ApiResponse<bool>> Update(UserCreateUpdateDTO userDTO)
  {
    try
    {
      if (userDTO == null || userDTO.Id == 0)
      {
        return new ApiResponse<bool>(false, "Invalid user data");
      }
          
      var user = await _context.User
        .Include(u => u.Address)
        .FirstOrDefaultAsync(u => u.Id == userDTO.Id);

      if (user == null)
      {
        return new ApiResponse<bool>(false, "User not found");
      }

      // todo update user with manual map for control by the best process
      user.Name = userDTO.Name;
      user.SurName = userDTO.SurName;
      user.Email = userDTO.Email;
      user.Phone = userDTO.Phone;
      user.Dob = userDTO.Dob;
      user.UpdateAt = DateTime.UtcNow;

      // only update the possword if is provided
       // todo hash the password
      if (!string.IsNullOrEmpty(userDTO.Password))
      {
          user.Password = userDTO.Password; // Aquí deberías hashear la contraseña
      }

      // Update or address if necessary
      user.Address.CountryId = userDTO.CountryId;
      user.Address.StateId = userDTO.StateId;
      user.Address.City = userDTO.City;
      user.Address.Street = userDTO.Street;
      user.Address.Line = userDTO.Line;
      user.Address.ZipCode = userDTO.ZipCode;
      user.Address.UpdateAt = DateTime.UtcNow;

      // todo save changes
      bool result = await Save();
      if (result)
      {
        _log.LogInformation("User updated successfully: ID {Id}.", userDTO.Id);
        return new ApiResponse<bool>(true, "User updated successfully", result);
      }
      else
      {
        _log.LogError("Error occurred while updating user with ID: {Id}", userDTO.Id);
        return new ApiResponse<bool>(false, "Error occurred while updating user");
      }
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Exception occurred while updating user with ID: {Id}", userDTO.Id);
      return new ApiResponse<bool>(false, "An exception occurred while updating user");
    }
  }

  //todo validate if users Exists
  private async Task<bool> Exists(string email)
  {
    try
    {
      return await _context.User.AnyAsync(a => a.Email == email);
    }
    catch (Exception ex)
    {

      _log.LogError(ex, "Error occurred while checking if user exists.");
      throw new Exception("Error occurred while checking if user exists.");

    }

  }
}