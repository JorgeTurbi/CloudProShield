using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using CloudShield.DTOs.UsersDTOs;
using Commons;
using Commons.Hash;
using DataContext;
using DTOs.UsersDTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Services.EmailServices;
using Services.SessionServices;
using Services.TokenServices;
using Services.UserServices;

namespace Repositories.Users;

public class UserRead : IUserCommandRead
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserRead> _log;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    private readonly ISessionCommandCreate _sessionCreate;

    public UserRead(ApplicationDbContext context, IMapper mapper, ILogger<UserRead> log, ISessionCommandCreate sessionCreate, ITokenService tokenService, IEmailService emailService)
    {
        _context = context;
        _mapper = mapper;
        _log = log;
        _sessionCreate = sessionCreate;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<ApiResponse<List<UserDTO_Only>>> GetAllUsers()
    {
        try
        {
            var users = await _context.User.ToListAsync();

            if (users == null || users.Count == 0)
            {
                return new ApiResponse<List<UserDTO_Only>>(false, "No users found", null);
            }

            var userDTOs = _mapper.Map<List<UserDTO_Only>>(users);
            return new ApiResponse<List<UserDTO_Only>>(true, "Users retrieved successfully", userDTOs);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error retrieving users");
            return new ApiResponse<List<UserDTO_Only>>(false, "An error occurred while retrieving users", null);

        }
    }

    public async Task<ApiResponse<UserDTO_Only>> GetUserByEmail(string email)
    {
        try
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return new ApiResponse<UserDTO_Only>(false, "User not found", null);
            }

            var userDTO = _mapper.Map<UserDTO_Only>(user);
            return new ApiResponse<UserDTO_Only>(true, "User retrieved successfully", userDTO);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error retrieving user by email");
            return new ApiResponse<UserDTO_Only>(false, "An error occurred while retrieving the user", null);
        }
    }
    public async Task<ApiResponse<string>> LoginUser(UserLoginDTO userLoginDTO, string ipAddress, string device)
    {
        try
        {

            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == userLoginDTO.Email);
            if (user == null)
            {
                return new ApiResponse<string>(false, "Invalid email or password", null);
            }
            bool veryfyPassword = PasswordHasher.VerifyPassword(user.Password, userLoginDTO.Password);
            if (!veryfyPassword)
            {
                return new ApiResponse<string>(false, "Invalid email or password", null);
            }

            // Generamos El token a 1 dia
            var userDTO = _mapper.Map<UserDTO>(user);
            var token = _tokenService.GenerateToken(userDTO, rememberMe: false);

            // Enviar notificación de inicio de sesión
            await _emailService.SendLoginNotificationAsync(
                user.Email,
                ipAddress,
                device);

            var sessionResponse = await _sessionCreate.CreateSession(user.Id, ipAddress, device);

            if (!sessionResponse.Success)
            {
                return new ApiResponse<string>(false, "Failed to create session", null);
            }

            // Assuming you want to return a token or some identifier upon successful login
            return new ApiResponse<string>(true, "Login successful", token);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error during user login");
            return new ApiResponse<string>(false, "An error occurred during login", null);
        }
    }
    public async Task<ApiResponse<UserDTO_Only>> GetUserById(int id)
    {
        try
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return new ApiResponse<UserDTO_Only>(false, "User not found", null);
            }

            var userDTO = _mapper.Map<UserDTO_Only>(user);
            return new ApiResponse<UserDTO_Only>(true, "User retrieved successfully", userDTO);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error retrieving user by ID");
            return new ApiResponse<UserDTO_Only>(false, "An error occurred while retrieving the user", null);
        }
    }
}


