using Commons;
using DTOs.UsersDTOs;

namespace Services.UserServices;


    public interface IUserCommandRead
    {
        Task<ApiResponse<UserDTO_Only>> GetUserById(int id);
        Task<ApiResponse<UserDTO_Only>> GetUserByEmail(string email);
        Task<ApiResponse<List<UserDTO_Only>>> GetAllUsers();
      
      
    }
