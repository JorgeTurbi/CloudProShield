using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using CloudShield.Services.OperationStorage;
using Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudProShield.Controllers;

/// <summary>
/// Legacy operations controller for user file management
/// Note: Consider migrating to the new specialized controllers (FilesControllerUser, FolderControllerUser)
/// </summary>
[ApiController]
[Route("api/operations/user")]
[Authorize]
public sealed class OperationControllerUser : ControllerBase
{
    private readonly IStorageServiceUser _storage;

    public OperationControllerUser(IStorageServiceUser storage) => _storage = storage;

    /// <summary>
    /// Download a file by relative path
    /// </summary>
    [HttpGet("download/{*relativePath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadFile(
        string relativePath,
        CancellationToken ct = default
    )
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        if (string.IsNullOrWhiteSpace(relativePath))
            return BadRequest(new ApiResponse<object>(false, "File path is required", null));

        try
        {
            // Decode and normalize path
            var decodedPath = WebUtility.UrlDecode(relativePath).Replace('\\', '/').TrimStart('/');

            // Get the file
            var (success, stream, contentType, reason) = await _storage.GetFileAsync(
                userId,
                decodedPath,
                ct
            );

            if (success)
            {
                var fileName = Path.GetFileName(decodedPath);
                return File(stream!, contentType ?? MediaTypeNames.Application.Octet, fileName);
            }

            return NotFound(new ApiResponse<object>(false, reason, null));
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(false, "Internal server error during file download", null)
            );
        }
    }

    /// <summary>
    /// Download folder as ZIP file
    /// </summary>
    [HttpGet("download-folder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadFolder(
        [FromQuery] string folder,
        CancellationToken ct = default
    )
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        if (string.IsNullOrWhiteSpace(folder))
            return BadRequest(new ApiResponse<object>(false, "Folder path is required", null));

        try
        {
            // Decode and normalize path
            var decodedPath = WebUtility.UrlDecode(folder).Replace('\\', '/').TrimStart('/');

            var zipBytes = await _storage.CreateFolderZipAsync(userId, decodedPath, ct);

            if (zipBytes == null)
                return NotFound(
                    new ApiResponse<object>(false, "Folder not found or is empty", null)
                );

            var folderName = Path.GetFileName(decodedPath) ?? "folder";
            return File(zipBytes, "application/zip", $"{folderName}.zip");
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(false, "Internal server error during folder download", null)
            );
        }
    }

    /// <summary>
    /// Legacy endpoint - redirects to new file download endpoint
    /// </summary>
    [HttpGet("{*relativePath}")]
    [ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger since it's legacy
    public async Task<IActionResult> LegacyDownload(
        string relativePath,
        CancellationToken ct = default
    )
    {
        // Redirect to the new download endpoint
        return await DownloadFile(relativePath, ct);
    }

    /// <summary>
    /// Get file metadata
    /// </summary>
    [HttpGet("metadata/{*relativePath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFileMetadata(
        string relativePath,
        CancellationToken ct = default
    )
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        if (string.IsNullOrWhiteSpace(relativePath))
            return BadRequest(new ApiResponse<object>(false, "File path is required", null));

        try
        {
            // Decode and normalize path
            var decodedPath = WebUtility.UrlDecode(relativePath).Replace('\\', '/').TrimStart('/');

            var metadata = await _storage.FindMetaAsyncUser(userId, decodedPath, ct);

            if (metadata == null)
                return NotFound(new ApiResponse<object>(false, "File metadata not found", null));

            return Ok(
                new ApiResponse<object>(true, "File metadata retrieved successfully", metadata)
            );
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(
                    false,
                    "Internal server error while retrieving file metadata",
                    null
                )
            );
        }
    }
}
