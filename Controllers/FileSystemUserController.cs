using System.Security.Claims;
using CloudShield.Services.FileSystemServices;
using Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Handles file system read operations for users
/// </summary>
[ApiController]
[Authorize]
[Route("api/user/filesystem")]
public class FileSystemReadControllerUser : ControllerBase
{
    private readonly IFileSystemReadServiceUser _fileSystemService;

    public FileSystemReadControllerUser(IFileSystemReadServiceUser fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    /// <summary>
    /// Get complete folder structure for the user
    /// </summary>
    [HttpGet("structure")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserFolderStructure(CancellationToken ct = default)
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        try
        {
            var result = await _fileSystemService.GetUserFolderStructureAsync(userId, ct);

            if (result.Success)
                return Ok(result);

            return NotFound(result);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(
                    false,
                    "Internal server error while retrieving folder structure",
                    null
                )
            );
        }
    }

    /// <summary>
    /// Get content of a specific folder
    /// </summary>
    [HttpGet("content/{*folderPath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFolderContent(
        string folderPath = "",
        [FromQuery] bool deep = false,
        CancellationToken ct = default
    )
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        try
        {
            // Decode and normalize path
            var decodedPath = string.IsNullOrWhiteSpace(folderPath)
                ? string.Empty
                : Uri.UnescapeDataString(folderPath).Replace('\\', '/').TrimStart('/');

            var result = await _fileSystemService.GetFolderContentAsync(userId, decodedPath, ct);

            if (result.Success)
                return Ok(result);

            return NotFound(result);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(
                    false,
                    "Internal server error while retrieving folder content",
                    null
                )
            );
        }
    }

    /// <summary>
    /// Get root level content for exploration
    /// </summary>
    [HttpGet("explore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetExploreContent(CancellationToken ct = default)
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        try
        {
            var result = await _fileSystemService.GetFolderContentExploreAsync(userId, ct);

            if (result.Success)
                return Ok(result);

            return NotFound(result);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(
                    false,
                    "Internal server error while retrieving explore content",
                    null
                )
            );
        }
    }

    /// <summary>
    /// Get all folders for the user (flat list)
    /// </summary>
    [HttpGet("folders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUserFolders(CancellationToken ct = default)
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        try
        {
            var result = await _fileSystemService.GetUserFoldersAsync(userId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(
                    false,
                    "Internal server error while retrieving folders",
                    null
                )
            );
        }
    }

    /// <summary>
    /// Get all files for the user
    /// </summary>
    [HttpGet("files")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUserFiles(CancellationToken ct = default)
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        try
        {
            var result = await _fileSystemService.GetAllUserFilesAsync(userId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(false, "Internal server error while retrieving files", null)
            );
        }
    }

    /// <summary>
    /// Get user's space information
    /// </summary>
    [HttpGet("space")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserSpace(CancellationToken ct = default)
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        try
        {
            var result = await _fileSystemService.GetAllSpaceAsync(userId.ToString(), ct);

            if (result.Success)
                return Ok(result);

            return NotFound(result);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(
                    false,
                    "Internal server error while retrieving space information",
                    null
                )
            );
        }
    }
}
