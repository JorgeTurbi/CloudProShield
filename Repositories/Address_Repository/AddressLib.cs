using AutoMapper;
using CloudShield.Entities.Entity_Address;
using Commons;
using DataContext;
using DTOs.Address_DTOS;
using DTOs.UsersDTOs;
using Microsoft.EntityFrameworkCore;
using Services.AddressServices;

namespace Repositories.Address_Repository;

public class AddressLib : IAddress
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<AddressLib> _log;

    public AddressLib(ApplicationDbContext context, IMapper mapper, ILogger<AddressLib> log)
    {
        _context = context;
        _mapper = mapper;
        _log = log;
    }

    public async Task<ApiResponse<bool>> AddNew(
        AddressDTOS addressDTO,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (addressDTO is null)
            {
                _log.LogError("AddressDTO is null.");
                return new ApiResponse<bool>(false, "AddressDTO is null.", false);
            }
            if (
                string.IsNullOrEmpty(addressDTO.City)
                || string.IsNullOrEmpty(addressDTO.Street)
                || string.IsNullOrEmpty(addressDTO.ZipCode)
            )
            {
                _log.LogError("Address fields are invalid.");
                return new ApiResponse<bool>(false, "Address fields are invalid.", false);
            }
            // Check if the user exists before adding the address
            var userExists = await _context.User.AnyAsync(
                u => u.Id == addressDTO.UserId,
                cancellationToken
            );
            if (!userExists)
            {
                _log.LogError("User's Address with ID {UserId} does not exist.", addressDTO.UserId);
                return new ApiResponse<bool>(false, "User's Address does not exist.", false);
            }
            // Check if the country exists before adding the address
            var countryExists = await _context.Country.AnyAsync(
                c => c.Id == addressDTO.CountryId,
                cancellationToken
            );
            if (!countryExists)
            {
                _log.LogError("Country with ID {CountryId} does not exist.", addressDTO.CountryId);
                return new ApiResponse<bool>(false, "Country does not exist.", false);
            }
            // Check if the state exists before adding the address
            var stateExists = await _context.State.AnyAsync(
                s => s.Id == addressDTO.StateId,
                cancellationToken
            );
            if (!stateExists)
            {
                _log.LogError("State with ID {StateId} does not exist.", addressDTO.StateId);
                return new ApiResponse<bool>(false, "State does not exist.", false);
            }
            // Check if the address already exists for the user
            var addressExists = await _context.Address.AnyAsync(
                a => a.UserId == addressDTO.UserId,
                cancellationToken
            );
            if (addressExists)
            {
                _log.LogError(
                    "Address already exists for user with ID {UserId}.",
                    addressDTO.UserId
                );
                return new ApiResponse<bool>(false, "Address already exists for user.", false);
            }
            // Map the AddressDTO to Address entity
            var address = _mapper.Map<Address>(addressDTO);
            address.CreateAt = DateTime.Now;
            // Add the address to the database
            await _context.Address.AddAsync(address);
            // Save changes to the database
            var result = await Save(cancellationToken);
            if (result)
            {
                _log.LogInformation(
                    "Address added successfully for user with ID {UserId}.",
                    addressDTO.UserId
                );
                return new ApiResponse<bool>(true, "Address added successfully.", true);
            }
            else
            {
                _log.LogError(
                    "Failed to add address for user with ID {UserId}.",
                    addressDTO.UserId
                );
                return new ApiResponse<bool>(false, "Failed to add address.", false);
            }
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            _log.LogError("An error occurred while adding address: {ex.Message}", ex);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    public async Task<ApiResponse<bool>> Delete(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var addressfromDto = GetById(id, cancellationToken).Result.Data;
            if (addressfromDto == null)
            {
                _log.LogError("Address with ID {Id} not found.", id);
                return new ApiResponse<bool>(false, "Address not found.", false);
            }
            var selected = _mapper.Map<Address>(addressfromDto);

            _context.Address.Remove(selected);
            var result = await Save(cancellationToken);
            if (true == result)
            {
                _log.LogInformation("Address with ID {Id} deleted successfully.", id);
                return new ApiResponse<bool>(true, "Address deleted successfully.", true);
            }
            else
            {
                _log.LogError("Failed to delete address with ID {Id}.", id);
                return new ApiResponse<bool>(false, "Failed to delete address.", false);
            }
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            _log.LogError(
                "An error occurred while deleting address with ID {Id}: {ex.Message}",
                id,
                ex
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    public async Task<ApiResponse<bool>> Exists(
        UserDTO user,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var exists = await _context.Address.AnyAsync(
                x => x.UserId == user.Id,
                cancellationToken
            );
            if (exists == true)
            {
                _log.LogInformation("Address exists for user {UserId}", user.Id);
                return new ApiResponse<bool>(
                    true,
                    "Address exists for user {UserId}",
                    user.Id > Guid.Empty
                );
            }
            else
            {
                _log.LogInformation("Address does not exist for user {UserId}", user.Id);
                // Log the absence of the address or handle it as needed
                return new ApiResponse<bool>(
                    false,
                    "Address does not exist for user {UserId}",
                    user.Id > Guid.Empty
                );
            }
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            _log.LogError(
                "An error occurred while checking if the address exists for user {UserId}",
                ex
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    public async Task<ApiResponse<List<AddressDTObyUser>>> GetAll(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            List<AddressDTObyUser> query = await (
                from a in _context.Address
                join u in _context.User on a.UserId equals u.Id
                join c in _context.Country on a.CountryId equals c.Id
                join s in _context.State on a.StateId equals s.Id
                select new AddressDTObyUser
                {
                    User = u.Email,
                    Country = c.Name,
                    State = s.Name,
                    City = a.City,
                    Street = a.Street,
                    Line = a.Line,
                    ZipCode = a.ZipCode,
                }
            ).ToListAsync(cancellationToken);
            if (query == null || query.Count == 0)
            {
                _log.LogInformation("No addresses found.");
                return new ApiResponse<List<AddressDTObyUser>>(false, "No addresses found.", null);
            }

            _log.LogInformation("Addresses retrieved successfully.");
            return new ApiResponse<List<AddressDTObyUser>>(
                true,
                "Addresses retrieved successfully.",
                query
            );
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            _log.LogError("An error occurred while retrieving addresses: {ex.Message}", ex);
            return new ApiResponse<List<AddressDTObyUser>>(false, ex.Message, null);
        }
    }

    public async Task<ApiResponse<AddressDTOS>> GetById(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var address = await _context.Address.FindAsync(id, cancellationToken);
            if (address == null)
            {
                _log.LogError("Address with ID {Id} not found.", id);
                return new ApiResponse<AddressDTOS>(false, "Address not found.", null);
            }

            var addressDTO = _mapper.Map<AddressDTOS>(address);
            _log.LogInformation("Address with ID {Id} retrieved successfully.", id);
            return new ApiResponse<AddressDTOS>(
                true,
                "Address retrieved successfully.",
                addressDTO
            );
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            _log.LogError(
                "An error occurred while retrieving address with ID {Id}: {ex.Message}",
                id,
                ex
            );
            return new ApiResponse<AddressDTOS>(false, ex.Message, null);
        }
    }

    private async Task<bool> Save(CancellationToken cancellationToken = default)
    {
        try
        {
            bool result = await _context.SaveChangesAsync(cancellationToken) > 0 ? true : false;
            if (result)
            {
                _log.LogInformation("Address saved successfully.");
                return result;
            }
            else
            {
                _log.LogError("Failed to save address.");
                return result;
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error occurred while saving the address.", ex);
        }
    }

    public Task<ApiResponse<bool>> Update(
        AddressDTOS addressDTO,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<AddressDTObyUser>> GetAddressbyUserId(
        Guid UserId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            AddressDTObyUser address = await (
                from a in _context.Address
                join u in _context.User on a.UserId equals u.Id
                join c in _context.Country on a.CountryId equals c.Id
                join s in _context.State on a.StateId equals s.Id
                where a.UserId == UserId
                select new AddressDTObyUser
                {
                    User = u.Email,
                    Country = c.Name,
                    State = s.Name,
                    City = a.City,
                    Street = a.Street,
                    Line = a.Line,
                    ZipCode = a.ZipCode,
                }
            ).FirstOrDefaultAsync(cancellationToken);
            if (address == null)
            {
                _log.LogError("Address not found for user with ID {UserId}.", UserId);
                return new ApiResponse<AddressDTObyUser>(false, "Address not found.", null);
            }

            _log.LogInformation(
                "Address retrieved successfully for user with ID {UserId}.",
                UserId
            );
            return new ApiResponse<AddressDTObyUser>(
                true,
                "Address retrieved successfully.",
                address
            );
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            _log.LogError(
                "An error occurred while retrieving address for user with ID {UserId}: {ex.Message}",
                UserId,
                ex
            );
            return new ApiResponse<AddressDTObyUser>(false, ex.Message, null);
        }
    }
}
