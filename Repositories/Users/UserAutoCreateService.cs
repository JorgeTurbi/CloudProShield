using AutoMapper;
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
        // Validaciones mejoradas
        if (string.IsNullOrWhiteSpace(userDTO.Email))
        {
            _log.LogError("Email Account Invalid {Email}", userDTO.Email);
            return new ApiResponse<bool>(false, "Email Account Invalid");
        }

        if (userDTO.Id == Guid.Empty)
        {
            _log.LogError("Invalid UserId provided: {UserId}", userDTO.Id);
            return new ApiResponse<bool>(false, "Invalid UserId");
        }

        var existingUser = await _context.User.FirstOrDefaultAsync(
            u => u.Id == userDTO.Id || u.Email.ToLower() == userDTO.Email.ToLower(),
            cancellationToken
        );

        if (existingUser != null)
        {
            _log.LogInformation(
                "User already exists in CloudShield - ID: {UserId}, Email: {Email}. No action needed.",
                userDTO.Id,
                userDTO.Email
            );
            // IMPORTANTE: Retornar success = false para indicar que NO se creó el usuario
            return new ApiResponse<bool>(false, "User already exists", false);
        }

        // Validar que existan Country y State
        if (!await ValidateCountryAndState(userDTO.CountryId, userDTO.StateId, cancellationToken))
        {
            _log.LogError(
                "Invalid CountryId: {CountryId} or StateId: {StateId}",
                userDTO.CountryId,
                userDTO.StateId
            );
            return new ApiResponse<bool>(false, "Invalid Country or State");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // IMPORTANTE: Hashear la contraseña aquí
            string hashedPassword = PasswordHasher.HashPassword(userDTO.PlainPassword);

            // Usar el GUID del AuthService como ID
            var user = new User
            {
                Id = userDTO.Id, // IMPORTANTE: Usar el mismo GUID del AuthService
                Name = userDTO.Name.Trim(),
                SurName = userDTO.SurName?.Trim() ?? string.Empty,
                Dob = userDTO.Dob ?? DateTime.UtcNow.AddYears(-25),
                Email = userDTO.Email.Trim().ToLowerInvariant(),
                Password = hashedPassword,
                Phone = userDTO.Phone?.Trim() ?? string.Empty,
                IsActive = true,
                Confirm = true,
                ConfirmToken = string.Empty,
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
                await transaction.RollbackAsync(cancellationToken);
                _log.LogError("Failed to save user {Email}", userDTO.Email);
                return new ApiResponse<bool>(false, "Failed to create user");
            }

            _log.LogInformation(
                "User auto-created from AuthService - ID: {UserId}, Email: {Email}",
                userDTO.Id,
                userDTO.Email
            );

            // Crear dirección con valores válidos
            var addressResult = await CreateAddressFromAuthService(userDTO, cancellationToken);
            if (!addressResult.Success)
            {
                await transaction.RollbackAsync(cancellationToken);
                _log.LogError(
                    "Failed to create address for user {Email}: {Message}",
                    userDTO.Email,
                    addressResult.Message
                );
                return new ApiResponse<bool>(
                    false,
                    $"Failed to create address: {addressResult.Message}"
                );
            }

            await transaction.CommitAsync(cancellationToken);

            _log.LogInformation("User and address successfully created for {Email}", userDTO.Email);
            return new ApiResponse<bool>(true, "User created successfully from AuthService");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _log.LogError(
                ex,
                "Error occurred while creating user from AuthService {Email}",
                userDTO.Email
            );
            return new ApiResponse<bool>(false, "Error occurred while creating user");
        }
    }

    /// <summary>
    /// Validar que existan Country y State en la base de datos
    /// </summary>
    private async Task<bool> ValidateCountryAndState(
        int countryId,
        int stateId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var countryExists = await _context
                .Set<Country>()
                .AnyAsync(c => c.Id == countryId, cancellationToken);
            if (!countryExists)
            {
                _log.LogWarning("Country with ID {CountryId} does not exist", countryId);
                return false;
            }

            var stateExists = await _context
                .Set<State>()
                .AnyAsync(s => s.Id == stateId && s.CountryId == countryId, cancellationToken);
            if (!stateExists)
            {
                _log.LogWarning(
                    "State with ID {StateId} does not exist for Country {CountryId}",
                    stateId,
                    countryId
                );
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error validating Country and State");
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

    /// <summary>
    /// Crear dirección usando los datos del AuthService
    /// </summary>
    private async Task<ApiResponse<bool>> CreateAddressFromAuthService(
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
                City = userDTO.City?.Trim() ?? "Miami", // Fallback por si viene vacío
                Street = userDTO.Street?.Trim() ?? "Main Street", // Fallback
                Line = userDTO.Line?.Trim(), // Puede ser null
                ZipCode = userDTO.ZipCode?.Trim() ?? "33101", // Fallback
            };

            _log.LogDebug(
                "Creating address from AuthService data: Country={CountryId}, State={StateId}, City={City}, Street={Street}",
                addressDTO.CountryId,
                addressDTO.StateId,
                addressDTO.City,
                addressDTO.Street
            );

            return await _addressLib.AddNew(addressDTO, cancellationToken);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error creating address from AuthService data for user {Email}",
                userDTO.Email
            );
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
