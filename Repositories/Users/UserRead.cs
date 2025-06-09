using AutoMapper;
using CloudShield.DTOs.UsersDTOs;
using Commons;
using Commons.Hash;
using DataContext;
using DTOs.UsersDTOs;
using Microsoft.EntityFrameworkCore;
using Services.EmailServices;
using Services.SessionServices;
using Services.UserServices;

namespace Repositories.Users;

public class UserRead : IUserCommandRead
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserRead> _log;
    private readonly IEmailService _emailService;
    private readonly ISessionCommandCreate _sessionCreate;
    private readonly IUserValidationService _userValidation;

    public UserRead(ApplicationDbContext context, IMapper mapper, ILogger<UserRead> log, ISessionCommandCreate sessionCreate, IEmailService emailService, IUserValidationService userValidation)
    {
        _context = context;
        _mapper = mapper;
        _log = log;
        _sessionCreate = sessionCreate;
        _emailService = emailService;
        _userValidation = userValidation;
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
            // 1. Validar usuario usando el servicio de validación
            var userValidation = await _userValidation.ValidateUserForLogin(userLoginDTO.Email, userLoginDTO.Password);

            if (!userValidation.Success)
            {
                // Log del intento de login fallido para seguridad
                _log.LogWarning("Failed login attempt for email: {Email} from IP: {IP}", userLoginDTO.Email, ipAddress);
                return new ApiResponse<string>(false, userValidation.Message, null);
            }

            var user = userValidation.Data;

            // 2. Crear sesión (aquí también se valida nuevamente en SessionCreate_Repository)
            var sessionResponse = await _sessionCreate.CreateSession(user.Id, ipAddress, device);

            if (!sessionResponse.Success)
            {
                _log.LogError("Failed to create session for user: {UserId}", user.Id);
                return new ApiResponse<string>(false, "Failed to create session", null);
            }

            // 3. Enviar notificación de inicio de sesión (solo si todo es exitoso)
            try
            {
                await _emailService.SendLoginNotificationAsync(user.Email, ipAddress, device);
            }
            catch (Exception emailEx)
            {
                // No fallar el login si el email no se puede enviar
                _log.LogWarning(emailEx, "Failed to send login notification email to {Email}", user.Email);
            }

            // 4. Log del login exitoso
            _log.LogInformation("Successful login for user: {Email} from IP: {IP}", user.Email, ipAddress);

            return new ApiResponse<string>(true, "Login successful", sessionResponse.Data.TokenRequest);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error during user login for email: {Email}", userLoginDTO.Email);
            return new ApiResponse<string>(false, "An error occurred during login", null);
        }
    }
    public async Task<ApiResponse<UserDTO_Only>> GetUserById(Guid id)
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

    public async Task<ApiResponse<UserDTO_Only>> GetProfile(Guid userId)
    {
        try
        {
            var user = await _context.User
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                return new(false, "User not found", null);

            // Sólo los campos que el front necesita
            var profile = new UserDTO_Only
            {
                Id = user.Id,
                Name = user.Name,
                SurName = user.SurName,
                Email = user.Email,
                Phone = user.Phone,
                Dob = user.Dob
            };

            return new(true, "Profile loaded", profile);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error getting profile");
            return new(false, "Unable to load profile", null);
        }
    }
}


