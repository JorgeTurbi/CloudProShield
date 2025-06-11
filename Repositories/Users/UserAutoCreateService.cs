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
using Services.UserServices;

namespace Repositories.Users;

public class UserAutoCreateService : IUserAutoCreateService, ISaveServices
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserAutoCreateService> _log;
    private readonly IAddress _addressLib;

    public UserAutoCreateService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<UserAutoCreateService> log,
        IAddress addressLib
    )
    {
        _context = context;
        _mapper = mapper;
        _log = log;
        _addressLib = addressLib;
    }

    public async Task<ApiResponse<bool>> CreateFromAuthServiceAsync(
        UserAutoCreateDTO userDTO,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(userDTO.Email))
        {
            _log.LogError("Email Account Invalid {Email}", userDTO.Email);
            return new ApiResponse<bool>(false, "Email Account Invalid");
        }

        // Verificar si el usuario ya existe
        if (await UserExists(userDTO.Id, userDTO.Email))
        {
            _log.LogInformation("User already exists in CloudShield {Email}", userDTO.Email);
            return new ApiResponse<bool>(true, "User already exists");
        }

        try
        {
            // IMPORTANTE: Hashear la contraseña aquí
            string hashedPassword = PasswordHasher.HashPassword(userDTO.PlainPassword);

            // Usar el GUID del AuthService como ID
            var user = new User
            {
                Id = userDTO.Id, // IMPORTANTE: Usar el mismo GUID del AuthService
                Name = userDTO.Name,
                SurName = userDTO.SurName,
                Dob = userDTO.Dob ?? DateTime.UtcNow.AddYears(-18), // Fecha por defecto si no viene
                Email = userDTO.Email,
                Password = hashedPassword, // Contraseña hasheada
                Phone = userDTO.Phone,
                IsActive = true, // Auto-activado desde AuthService
                Confirm = true, // Auto-confirmado desde AuthService
                ConfirmToken = string.Empty, // No necesario para cuentas auto-creadas
                CreateAt = DateTime.UtcNow,
                ResetPasswordToken = string.Empty,
                ResetPasswordExpires = DateTime.MinValue,
                Otp = string.Empty,
                OtpExpires = DateTime.MinValue,
                SpaceCloud = await CreateSpaceCloud(userDTO.Plan, userDTO.Id),
            };

            await _context.User.AddAsync(user, cancellationToken);
            bool userSaved = await Save(cancellationToken);

            if (!userSaved)
            {
                _log.LogError("Failed to save user {Email}", userDTO.Email);
                return new ApiResponse<bool>(false, "Failed to create user");
            }

            _log.LogInformation("User auto-created from AuthService {Email}", userDTO.Email);

            // Crear dirección con valores válidos
            var addressResult = await CreateDefaultAddress(userDTO, cancellationToken);
            if (!addressResult.Success)
            {
                _log.LogError(
                    "Failed to create address for user {Email}: {Message}",
                    userDTO.Email,
                    addressResult.Message
                );
                // Para debugging, logear los valores que se están enviando
                _log.LogDebug(
                    "Address values: CountryId={CountryId}, StateId={StateId}, City={City}, Street={Street}, ZipCode={ZipCode}",
                    userDTO.CountryId,
                    userDTO.StateId,
                    userDTO.City,
                    userDTO.Street,
                    userDTO.ZipCode
                );
            }
            else
            {
                _log.LogInformation("Address created successfully for user {Email}", userDTO.Email);
            }

            return new ApiResponse<bool>(true, "User created successfully from AuthService");
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error occurred while creating user from AuthService {Email}",
                userDTO.Email
            );
            return new ApiResponse<bool>(false, "Error occurred while creating user");
        }
    }

    private async Task<bool> UserExists(Guid userId, string email)
    {
        try
        {
            return await _context.User.AnyAsync(u => u.Id == userId || u.Email == email);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error checking if user exists");
            return false;
        }
    }

    private async Task<SpaceCloud> CreateSpaceCloud(string plan, Guid userId)
    {
        return new SpaceCloud
        {
            Id = Guid.NewGuid(),
            MaxBytes = plan switch
            {
                "basic" => 5L * 1024 * 1024 * 1024, // 5 GB
                "pro" => 100L * 1024 * 1024 * 1024, // 100 GB
                "enterprise" => long.MaxValue,
                _ => 5L * 1024 * 1024 * 1024, // Default a basic
            },
            UsedBytes = 0,
            UserId = userId,
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
            RowVersion = Array.Empty<byte>(),
        };
    }

    private async Task<ApiResponse<bool>> CreateDefaultAddress(
        UserAutoCreateDTO userDTO,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var addressDTO = new AddressDTOS
            {
                Id = Guid.NewGuid(),
                UserId = userDTO.Id,
                CountryId = userDTO.CountryId,
                StateId = userDTO.StateId,
                City = userDTO.City,
                Street = userDTO.Street,
                Line = userDTO.Line,
                ZipCode = userDTO.ZipCode,
            };

            _log.LogDebug(
                "Creating address with values: {AddressDTO}",
                System.Text.Json.JsonSerializer.Serialize(addressDTO)
            );

            return await _addressLib.AddNew(addressDTO, cancellationToken);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error creating default address for user {Email}", userDTO.Email);
            return new ApiResponse<bool>(false, $"Error creating address: {ex.Message}");
        }
    }

    public async Task<bool> Save(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error occurred while saving changes");
            throw;
        }
    }
}
