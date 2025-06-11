using AutoMapper;
using CloudShield.DTOs.FileSystem;
using CloudShield.Entities.Operations;
using Commons;
using Commons.Hash;
using Commons.Utils;
using DataContext;
using DTOs.Address_DTOS;
using DTOs.UsersDTOs;
using Entities.Users;
using Microsoft.EntityFrameworkCore;
using Services.AddressServices;
using Services.EmailServices;
using Services.TokenServices;
using Services.UserServices;

namespace CloudShield.Repositories.Users;

public class UserLib : IUserCommandCreate, ISaveServices
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserLib> _log;
    private readonly IAddress _addressLib;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    //todo  the user's Contructor
    public UserLib(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<UserLib> log,
        IAddress addressLib,
        ITokenService tokenService,
        IEmailService emailService
    )
    {
        _context = context;
        _mapper = mapper;
        _log = log;
        _addressLib = addressLib;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    //todo Create a new User, it response an object type ApiResponse with boolean data
    public async Task<ApiResponse<bool>> AddNew(
        UserDTO userDTO,
        string? originUrl,
        CancellationToken cancellationToken = default
    )
    {
        //todo validations if user is empty or null
        if (string.IsNullOrEmpty(userDTO.Email))
        {
            //!realizar el log
            _log.LogError("Email Account Invalid {Email}", userDTO.Email);
            return new ApiResponse<bool>(false, "Email Account Invalid");
        }

        // Generate token before mapping
        string confirmToken = _tokenService.GenerateToken(userDTO, false);

        // Hash password
        string hashedPassword = PasswordHasher.HashPassword(userDTO.Password);

        // Create User entity with explicit property assignment to handle required properties
        var user = new User
        {
            Name = userDTO.Name,
            SurName = userDTO.SurName,
            Dob = userDTO.Dob,
            Email = userDTO.Email,
            Password = hashedPassword,
            Phone = userDTO.Phone,
            IsActive = false, // Set to false initially, will be activated after email confirmation
            Confirm = false, // Set to false initially, will be true after email confirmation
            ConfirmToken = confirmToken,
            CreateAt = DateTime.UtcNow,
            ResetPasswordToken = string.Empty, // Initialize to avoid null issues
            ResetPasswordExpires = DateTime.MinValue,
            Otp = string.Empty, // Initialize to avoid null issues
            OtpExpires = DateTime.MinValue,
            SpaceCloud =
                userDTO.Plan != null
                    ? await ValidatePlan(userDTO.Plan, userDTO.Id)
                    : new SpaceCloud
                    {
                        MaxBytes = 0,
                        UsedBytes = 0,
                        UserId = Guid.Empty,
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow,
                        RowVersion = Array.Empty<byte>(),
                    },
        };

        try
        {
            await _context.User.AddAsync(user, cancellationToken);
            bool result = await Save(cancellationToken);

            if (!result)
            {
                _log.LogError("Failed to save user {Email}", userDTO.Email);
                return new ApiResponse<bool>(false, "Failed to create user");
            }

            _log.LogInformation("User Registered {Email}", userDTO.Email);

            // Send confirmation email
            await _emailService.SendConfirmationEmailAsync(userDTO.Email, confirmToken, originUrl!);

            // Create address after user is successfully saved
            var selectedAddress = _mapper.Map<AddressDTOS>(userDTO);
            selectedAddress.UserId = user.Id;

            ApiResponse<bool> responseAddress = await _addressLib.AddNew(
                selectedAddress,
                cancellationToken
            );

            if (responseAddress?.Data != true)
            {
                _log.LogError(
                    "Error occurred while saving the address for user {Email}",
                    userDTO.Email
                );
                return new ApiResponse<bool>(false, "User created but address save failed");
            }

            _log.LogInformation("Address was saved for user {Email}", userDTO.Email);
            return new ApiResponse<bool>(true, "User Created Successfully");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error occurred while creating user {Email}", userDTO.Email);
            return new ApiResponse<bool>(false, "Error occurred while creating user");
        }
    }

    private async Task<SpaceCloud> ValidatePlan(string plan, Guid userId)
    {
        //todo validate if user is null
        if (plan == null)
        {
            _log.LogError("plan  null");
            return new SpaceCloud
            {
                MaxBytes = 0,
                UsedBytes = 0,
                UserId = Guid.Empty,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                RowVersion = Array.Empty<byte>(),
            };
        }

        return new SpaceCloud
        {
            MaxBytes = plan switch
            {
                "basic" => 5L * 1024 * 1024 * 1024, // 5 GB
                "pro" => 100L * 1024 * 1024 * 1024, // 100 GB
                "enterprise" => long.MaxValue, // Unlimited for Enterprise
                _ => throw new ArgumentException("Invalid plan type"),
            },
            Id = Guid.NewGuid(), // Generate a new ID for the SpaceCloud
            UsedBytes = 0,
            UserId = userId, // This will be set later when the user is created
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
            RowVersion = Array.Empty<byte>(), // Initialize concurrency token
        };
    }

    //todo to save data
    public async Task<bool> Save(CancellationToken cancellationToken = default)
    {
        try
        {
            bool result = await _context.SaveChangesAsync(cancellationToken) > 0 ? true : false;
            if (result)
            {
                return result;
            }
            else
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error occurred while saving the user.");
            throw new Exception("Error occurred while checking if user exists.");
        }
    }

    // //todo update user
    // public async Task<ApiResponse<bool>> Update(UserDTO userDTO)
    // {
    //   if (userDTO == null || userDTO.Id == 0)
    //   {
    //     return new ApiResponse<bool>(false, "Invalid user data");
    //   }

    //   var user = await _context.User.FindAsync(userDTO.Id);
    //   if (user == null)
    //   {
    //     return new ApiResponse<bool>(false, "User not found");
    //   }

    //   // todo validate email for no repeat
    //   if (!string.Equals(user.Email, userDTO.Email, StringComparison.OrdinalIgnoreCase))
    //   {
    //     bool emailExists = await _context.User.AnyAsync(u => u.Email == userDTO.Email && u.Id != userDTO.Id);
    //     //todo validate if email exists
    //     if (emailExists)
    //     {
    //       return new ApiResponse<bool>(false, "Email already exists");
    //     }
    //   }

    //   //todo use AutoMapper to map the properties
    //   _mapper.Map(userDTO, user);

    //   //todo update datetime
    //   user.UpdateAt = DateTime.UtcNow;

    //   // todo save changes
    //   bool result = await Save();
    //   if (result)
    //   {
    //     _log.LogInformation("User updated successfully");
    //     return new ApiResponse<bool>(true, "User updated successfully", result);
    //   }
    //   else
    //   {
    //     _log.LogError("Error occurred while updating user with ID: {Id}", userDTO.Id);
    //     return new ApiResponse<bool>(false, "Error occurred while updating user");
    //   }
    // }

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

    async Task<ApiResponse<bool>> IUserCommandCreate.ConfirmEmailAsync(string token)
    {
        try
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.ConfirmToken == token);
            if (user == null)
            {
                _log.LogWarning("Invalid or expired confirmation token");
                return new ApiResponse<bool>(false, "Invalid or expired confirmation token");
            }

            user.Confirm = true;
            user.IsActive = true;
            user.UpdateAt = DateTime.Now;

            _context.User.Update(user);
            bool result = await Save();

            if (result)
            {
                _log.LogInformation("User email confirmed for {Email}", user.Email);

                // Send confirmation email
                await _emailService.SendAccountConfirmedAsync(user.Email);

                return new ApiResponse<bool>(true, "Email confirmed successfully");
            }

            return new ApiResponse<bool>(false, "Failed to confirm email");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error while confirming email");
            return new ApiResponse<bool>(false, "Error while confirming email");
        }
    }
}
