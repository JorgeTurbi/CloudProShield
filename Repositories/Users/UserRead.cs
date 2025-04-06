using AutoMapper;
using Commons;
using DataContext;
using DTOs.UsersDTOs;
using Microsoft.EntityFrameworkCore;
using Services.UserServices;

namespace Repositories.Users;

public class UserRead : IUserCommandRead
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserRead> _log;

    public UserRead(ApplicationDbContext context, IMapper mapper, ILogger<UserRead> log)
    {
        _context = context;
        _mapper = mapper;
        _log = log;
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
}
