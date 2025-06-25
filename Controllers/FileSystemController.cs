using CloudShield.DTOs.FileSystem;
using CloudShield.Services.FileSystemServices;
using CloudShield.Services.OperationStorage;
using Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudShield.Controllers;

/// <summary>
/// Controlador para gestionar la estructura de carpetas y archivos del sistema de almacenamiento
/// </summary>
[ApiController]
[Route("api/customers/{customerId:guid}/filesystem")]
[Authorize]
public class FileSystemController : ControllerBase
{
    private readonly IFileSystemReadService _fileSystemService;
    private readonly IStorageService _storage;
    private readonly ILogger<FileSystemController> _logger;

    public FileSystemController(
        IFileSystemReadService fileSystemService,
        ILogger<FileSystemController> logger,
        IStorageService storage
    )
    {
        _fileSystemService = fileSystemService;
        _logger = logger;
        _storage = storage;
    }

    [HttpPost("folders")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFolder(
        Guid customerId,
        [FromBody] NewFolderDTO dto,
        CancellationToken ct = default
    )
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Nombre requerido" });

        var relPath = string.IsNullOrWhiteSpace(dto.ParentPath)
            ? dto.Name
            : $"{dto.ParentPath.TrimEnd('/')}/{dto.Name}";

        var resp = await _storage.CreateFolderAsync(customerId, relPath, ct);
        return resp.Success ? StatusCode(201, resp) : BadRequest(resp);
    }

    [HttpDelete("folders/{*folderPath}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteFolder(
        Guid customerId,
        string folderPath,
        CancellationToken ct = default
    )
    {
        folderPath = Uri.UnescapeDataString(folderPath ?? "").Trim('/');
        var (ok, reason) = await _storage.DeleteFolderAsync(customerId, folderPath, ct);
        return ok ? NoContent() : BadRequest(new { error = reason });
    }

    /// <summary>
    /// Obtiene la estructura completa de carpetas de un cliente
    /// </summary>
    /// <param name="customerId">ID del cliente</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Estructura de carpetas del cliente</returns>
    [HttpGet("structure")]
    [ProducesResponseType(typeof(ApiResponse<CustomerFolderStructureDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<CustomerFolderStructureDTO>),
        StatusCodes.Status404NotFound
    )]
    [ProducesResponseType(
        typeof(ApiResponse<CustomerFolderStructureDTO>),
        StatusCodes.Status500InternalServerError
    )]
    public async Task<IActionResult> GetCustomerFolderStructure(
        Guid customerId,
        CancellationToken ct = default
    )
    {
        try
        {
            _logger.LogInformation(
                "Solicitando estructura de carpetas para cliente {CustomerId}",
                customerId
            );

            var result = await _fileSystemService.GetCustomerFolderStructureAsync(customerId, ct);

            if (!result.Success)
            {
                return result.Data == null ? NotFound(result) : BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error no controlado al obtener estructura para cliente {CustomerId}",
                customerId
            );
            var errorResponse = new ApiResponse<CustomerFolderStructureDTO>(
                false,
                "Error interno del servidor",
                null
            );
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene todas las carpetas disponibles para un cliente
    /// </summary>
    /// <param name="customerId">ID del cliente</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Lista de carpetas del cliente</returns>
    [HttpGet("folders")]
    [ProducesResponseType(typeof(ApiResponse<List<FolderDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<FolderDTO>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<List<FolderDTO>>),
        StatusCodes.Status500InternalServerError
    )]
    public async Task<IActionResult> GetCustomerFolders(
        Guid customerId,
        CancellationToken ct = default
    )
    {
        try
        {
            _logger.LogInformation("Solicitando carpetas para cliente {CustomerId}", customerId);

            var result = await _fileSystemService.GetCustomerFoldersAsync(customerId, ct);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error no controlado al obtener carpetas para cliente {CustomerId}",
                customerId
            );
            var errorResponse = new ApiResponse<List<FolderDTO>>(
                false,
                "Error interno del servidor",
                new List<FolderDTO>()
            );
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene el contenido de una carpeta específica (Firms o Documents)
    /// </summary>
    /// <param name="customerId">ID del cliente</param>
    /// <param name="folderName">Nombre de la carpeta (Firms o Documents)</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Contenido de la carpeta especificada</returns>
    [HttpGet("folders/{*folderPath}")]
    [ProducesResponseType(typeof(ApiResponse<FolderContentDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FolderContentDTO>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FolderContentDTO>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<FolderContentDTO>),
        StatusCodes.Status500InternalServerError
    )]
    public async Task<IActionResult> GetFolderContent(
        Guid customerId,
        string folderPath,
        CancellationToken ct = default
    )
    {
        folderPath = Uri.UnescapeDataString(folderPath ?? "").Trim('/');
        if (string.IsNullOrWhiteSpace(folderPath))
            return BadRequest(new { error = "El nombre de la carpeta es requerido" });

        var result = await _fileSystemService.GetFolderContentAsync(customerId, folderPath, ct);
        return result.Success ? Ok(result)
            : result.Data == null ? NotFound(result)
            : BadRequest(result);
    }

    /// <summary>
    /// Obtiene todos los archivos de un cliente independientemente de la carpeta
    /// </summary>
    /// <param name="customerId">ID del cliente</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Lista de todos los archivos del cliente</returns>
    [HttpGet("files")]
    [ProducesResponseType(typeof(ApiResponse<List<FileItemDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<FileItemDTO>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<List<FileItemDTO>>),
        StatusCodes.Status500InternalServerError
    )]
    public async Task<IActionResult> GetAllCustomerFiles(
        Guid customerId,
        CancellationToken ct = default
    )
    {
        try
        {
            _logger.LogInformation(
                "Solicitando todos los archivos para cliente {CustomerId}",
                customerId
            );

            var result = await _fileSystemService.GetAllCustomerFilesAsync(customerId, ct);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error no controlado al obtener archivos para cliente {CustomerId}",
                customerId
            );
            var errorResponse = new ApiResponse<List<FileItemDTO>>(
                false,
                "Error interno del servidor",
                new List<FileItemDTO>()
            );
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }


  
}
