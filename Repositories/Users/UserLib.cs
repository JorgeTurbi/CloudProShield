using AutoMapper;
using CloudShield.Entities.Entity_Address;
using Commons;
using Commons.Utils;
using DataContext;
using DTOs.Address_DTOS;
using DTOs.UsersDTOs;
using Entities.Users;
using Microsoft.EntityFrameworkCore;
using Services.AddressServices;
using Services.UserServices;

namespace CloudShield.Repositories.Users;
public class UserLib : IUserCommandCreate, ISaveServices
{

  private readonly ApplicationDbContext _context;
  private readonly IMapper _mapper;
  private readonly ILogger<UserLib> _log;
  private readonly IAddress _addressLib;

  //todo  the user's Contructor
  public UserLib(ApplicationDbContext context, IMapper mapper, ILogger<UserLib> log , IAddress addressLib)
  {
    _context = context;
    _mapper = mapper;
    _log = log;
    _addressLib = addressLib;
  }

  //todo Create a new User, it response an object type ApiResponse with boolean data
  public async Task<ApiResponse<bool>> AddNew(UserDTO userDTO, CancellationToken cancellationToken = default)
  {
    //todo validations if user is empty or null

    if (string.IsNullOrEmpty(userDTO.Email))
    {
      //!realizar el log
      _log.LogError("Email Account Invalid {Email}", userDTO.Email);
      return new ApiResponse<bool>(false, "Email Account Invalid");


    }
    //todo validate if exists email account
    var reponse = await Exists(userDTO);
    if (reponse)
    {
        //!realizar el log
          _log.LogError("Email Exists {Email}", userDTO.Email);
      return new ApiResponse<bool>(false, "User Exists");
    }

    //todo mapping before save in database

    var Selected = _mapper.Map<User>(userDTO);

    await _context.User.AddAsync(Selected);
    bool result = await Save();
    _log.LogInformation("Se registro el usuario exitosamente {Email}", userDTO.Email);
  

    var SelectedAddress = _mapper.Map<AddressDTOS>(userDTO);

    ApiResponse<bool> ResponseAddress = await _addressLib.AddNew(SelectedAddress, cancellationToken);
    if (ResponseAddress == null || ResponseAddress.Data == false)
    {
      _log.LogError("Error occurred while saving the address for user {Email}", userDTO.Email);
      return new ApiResponse<bool>(false, "Error occurred while saving the address for user");
    }

    else{
      _log.LogInformation("Address was saved for user {Email}", userDTO.Email);
      return new ApiResponse<bool>(true, "User Created Successfully");
    }
   
   
  

  }

  //todo to save data
  public async Task<bool> Save(CancellationToken cancellationToken = default)
  {
    try
    {
    bool result =await _context.SaveChangesAsync(cancellationToken) > 0 ? true : false;
    if (result)
    {
        _log.LogError("User Registered {Email}", _context.User.LastOrDefault().Email);
      return true;
    }
    else
    {
        _log.LogError("User cannot Register {Email}", _context.User.LastOrDefault().Email);
      return false;
    }
   
    }
    catch (Exception ex)
    {

      _log.LogError(ex, "Error occurred while saving the user." );
       throw new Exception("Error occurred while checking if user exists.");
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