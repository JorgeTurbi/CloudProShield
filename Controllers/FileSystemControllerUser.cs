using CloudShield.DTOs.FileSystem;
using CloudShield.Entities.Operations;
using CloudShield.Services.FileSystemServices;
using Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudShield.Controllers;

/// <summary>
/// Controlador para gestionar la estructura de carpetas y archivos del sistema de almacenamiento
/// </summary>
[ApiController]
[Route("api/usuario/{userId:guid}/filesystem")]
[Authorize]
public class FileSystemControllerUser : ControllerBase
{
  private readonly IFileSystemReadServiceUser _fileSystemService;
  private readonly ILogger<FileSystemControllerUser> _logger;

  public FileSystemControllerUser(
      IFileSystemReadServiceUser fileSystemService,
      ILogger<FileSystemControllerUser> logger)
  {
    _fileSystemService = fileSystemService;
    _logger = logger;
  }

  /// <summary>
  /// Obtiene la estructura completa de carpetas de un usuario
  /// </summary>
  /// <param name="userId">ID del usuario</param>
  /// <param name="ct">Token de cancelación</param>
  /// <returns>Estructura de carpetas del usuario</returns>
  [HttpGet("structure")]
  [ProducesResponseType(typeof(ApiResponse<CustomerFolderStructureDTO>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<CustomerFolderStructureDTO>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<CustomerFolderStructureDTO>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetCustomerFolderStructure(
      Guid userId,
      CancellationToken ct = default)
  {
    try
    {
      _logger.LogInformation("Solicitando estructura de carpetas para usuario {UserId}", userId);

      var result = await _fileSystemService.GetCustomerFolderStructureAsync(userId, ct);

      if (!result.Success)
      {
        return result.Data == null ? NotFound(result) : BadRequest(result);
      }

      return Ok(result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error no controlado al obtener estructura para usuario {UserId}", userId);
      var errorResponse = new ApiResponse<CustomerFolderStructureDTO>(
          false,
          "Error interno del servidor",
          null);
      return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
    }
  }

  /// <summary>
  /// Obtiene todas las carpetas disponibles para un usuario
  /// </summary>
  /// <param name="userId">ID del usuario</param>
  /// <param name="ct">Token de cancelación</param>
  /// <returns>Lista de carpetas del usuario</returns>
  [HttpGet("folders")]
  [ProducesResponseType(typeof(ApiResponse<List<FolderDTO>>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<List<FolderDTO>>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<List<FolderDTO>>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetCustomerFolders(
      Guid userId,
      CancellationToken ct = default)
  {
    try
    {
      _logger.LogInformation("Solicitando carpetas para usuario {UserId}", userId);

      var result = await _fileSystemService.GetCustomerFoldersAsync(userId, ct);

      if (!result.Success)
      {
        return NotFound(result);
      }

      return Ok(result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error no controlado al obtener carpetas para usuario {UserId}", userId);
      var errorResponse = new ApiResponse<List<FolderDTO>>(
          false,
          "Error interno del servidor",
          new List<FolderDTO>());
      return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
    }
  }

  /// <summary>
  /// Obtiene el contenido de una carpeta específica (Firms o Documents)
  /// </summary>
  /// <param name="userId">ID del usuario</param>
  /// <param name="folderName">Nombre de la carpeta (Firms o Documents)</param>
  /// <param name="ct">Token de cancelación</param>
  /// <returns>Contenido de la carpeta especificada</returns>
  [HttpGet("folders/{folderName}")]
  [ProducesResponseType(typeof(ApiResponse<FolderContentDTO>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<FolderContentDTO>), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse<FolderContentDTO>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<FolderContentDTO>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetFolderContent(
      Guid userId,
      string folderName,
      CancellationToken ct = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(folderName))
      {
        var badRequestResponse = new ApiResponse<FolderContentDTO>(
            false,
            "El nombre de la carpeta es requerido",
            null);
        return BadRequest(badRequestResponse);
      }

      _logger.LogInformation(
          "Solicitando contenido de carpeta {FolderName} para usuario {UserId}",
          folderName,
          userId);

      var result = await _fileSystemService.GetFolderContentAsync(userId, folderName, ct);

      if (!result.Success)
      {
        return result.Data == null ? NotFound(result) : BadRequest(result);
      }

      return Ok(result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex,
          "Error no controlado al obtener contenido de carpeta {FolderName} para usuario {UserId}",
          folderName,
          userId);
      var errorResponse = new ApiResponse<FolderContentDTO>(
          false,
          "Error interno del servidor",
          null);
      return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
    }
  }

  /// <summary>
  /// Obtiene todos los archivos de un usuario independientemente de la carpeta
  /// </summary>
  /// <param name="userId">ID del usuario</param>
  /// <param name="ct">Token de cancelación</param>
  /// <returns>Lista de todos los archivos del usuario</returns>
  [HttpGet("files")]
  [ProducesResponseType(typeof(ApiResponse<List<FileItemDTO>>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<List<FileItemDTO>>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<List<FileItemDTO>>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetAllCustomerFiles(
      Guid userId,
      CancellationToken ct = default)
  {
    try
    {
      _logger.LogInformation("Solicitando todos los archivos para usuario {UserId}", userId);

      var result = await _fileSystemService.GetAllCustomerFilesAsync(userId, ct);

      if (!result.Success)
      {
        return NotFound(result);
      }

      return Ok(result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error no controlado al obtener archivos para usuario {UserId}", userId);
      var errorResponse = new ApiResponse<List<FileItemDTO>>(
          false,
          "Error interno del servidor",
          new List<FileItemDTO>());
      return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
    }
  }


  [HttpGet("storage")]
  [ProducesResponseType(typeof(ApiResponse<SpaceCloud>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<SpaceCloud>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<SpaceCloud>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetAllStorage(
      Guid userId,
      CancellationToken ct = default)
  {
    try
    {
      _logger.LogInformation("Solicitando todos el almacenamiento del  {UserId}", userId);

      var result = await _fileSystemService.GetAllSpaceAsync(userId, ct);

      if (!result.Success)
      {
        return NotFound(result);
      }

      return Ok(result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error no controlado al obtener storage {UserId}", userId);
      var errorResponse = new ApiResponse<SpaceCloud>(
          false,
          "Error interno del servidor");
      return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
    }
  }
}
