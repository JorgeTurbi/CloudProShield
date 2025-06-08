using Commons;
using DTOs.Address_DTOS;
using DTOs.UsersDTOs;

namespace Services.AddressServices
{
    public interface IAddress
    {
        Task<ApiResponse<bool>> AddNew(AddressDTOS addressDTO, CancellationToken cancellationToken = default);
        
        Task<ApiResponse<bool>> Exists(UserDTO user, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> Update(AddressDTOS addressDTO, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> Delete(Guid id, CancellationToken cancellationToken = default);
        Task<ApiResponse<AddressDTOS>> GetById(Guid id, CancellationToken cancellationToken = default);
        Task<ApiResponse<List<AddressDTObyUser>>> GetAll(CancellationToken cancellationToken = default);
        Task<ApiResponse<AddressDTObyUser>> GetAddressbyUserId(Guid UserId, CancellationToken cancellationToken = default);
    }
}