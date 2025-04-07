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
using Services.UserServices;

namespace Repositories.Users;

public class UserRead : IUserCommandRead
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserRead> _log;
    private readonly IConfiguration _configuration;
 
    public UserRead(ApplicationDbContext context, IMapper mapper, ILogger<UserRead> log, IConfiguration configuration )
    
       
    {
        _context = context;
        _mapper = mapper;
        _log = log;
        _configuration = configuration;
        
       
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
        public async Task<ApiResponse<string>> LoginUser(UserLoginDTO userLoginDTO)
        {
            try
            {
            
                var user = await _context.User.FirstOrDefaultAsync(u => u.Email == userLoginDTO.Email);
                 if (user == null)
                {
                    return new ApiResponse<string>(false, "Invalid email or password", null);
                }
                bool veryfyPassword = PasswordHasher.VerifyPassword(  user.Password,userLoginDTO.Password);
                if (!veryfyPassword)
                {
                    return new ApiResponse<string>(false, "Invalid email or password", null);
                }
                var userDTO = _mapper.Map<UserDTO>(user);
                // Generate JWT token
                var token = GenerateToken(userDTO, false);

                     // Assuming you want to return a token or some identifier upon successful login
                return new ApiResponse<string>(true, "Login successful", token);


               

               
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error during user login");
                return new ApiResponse<string>(false, "An error occurred during login", null);
            }
        }
    public async  Task<ApiResponse<UserDTO_Only>> GetUserById(int id)
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


        private string GenerateToken(UserDTO user, bool rememberMe)
        {

                var _key = _configuration["JwtSettings:SecretKey"];
    if (string.IsNullOrEmpty(_key))
    {
        return null;
    }


           var tokenHandler = new JwtSecurityTokenHandler();
    var keyBytes = Encoding.UTF8.GetBytes(_key);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        }),
        Expires = rememberMe
            ? DateTime.UtcNow.AddDays(14)   // Token dura 14 días si marcó "Remember Me"
            : DateTime.UtcNow.AddHours(1),  // Token dura 1 hora si no
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
    }
        }


