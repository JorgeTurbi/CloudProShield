using System.Security.Claims;
using CloudShield.Services.OperationStorage;
using Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Manages folder operations for users
/// </summary>
[ApiController]
[Authorize]
[Route("api/user/folders")]
public class FolderControllerUser : ControllerBase
{
    private readonly IStorageServiceUser _storage;

    public FolderControllerUser(IStorageServiceUser storage) => _storage = storage;

    /// <summary>
    /// Create a new folder in user's storage
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateFolder(
        [FromBody] CreateFolderRequest request,
        CancellationToken ct = default
    )
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        if (string.IsNullOrWhiteSpace(request?.FolderPath))
            return BadRequest(new ApiResponse<object>(false, "Folder path is required", null));

        try
        {
            var result = await _storage.CreateFolderAsync(userId, request.FolderPath, ct);

            if (result.Success)
            {
                return CreatedAtAction(
                    nameof(GetFolderContent),
                    new { folderPath = request.FolderPath },
                    result
                );
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(false, "Internal server error during folder creation", null)
            );
        }
    }

    /// <summary>
    /// Delete a folder and all its contents
    /// </summary>
    [HttpDelete("{*folderPath}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteFolder(string folderPath, CancellationToken ct = default)
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        if (string.IsNullOrWhiteSpace(folderPath))
            return BadRequest(new ApiResponse<object>(false, "Folder path is required", null));

        try
        {
            // Decode and normalize path
            var decodedPath = Uri.UnescapeDataString(folderPath).Replace('\\', '/').TrimStart('/');

            var (success, reason) = await _storage.DeleteFolderAsync(userId, decodedPath, ct);

            if (success)
                return NoContent();

            return BadRequest(new ApiResponse<object>(false, reason, null));
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse<object>(false, "Internal server error during folder deletion", null)
            );
        }
    }

    /// <summary>
    /// Download folder as ZIP file
    /// </summary>
    [HttpGet("download/{*folderPath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadFolderAsZip(
        string folderPath,
        CancellationToken ct = default
    )
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );

        if (string.IsNullOrWhiteSpace(folderPath))
            return BadRequest(new ApiResponse<object>(false, "Folder path is required", null));

        try
        {
            // Decode and normalize path
            var decodedPath = Uri.UnescapeDataString(folderPath).Replace('\\', '/').TrimStart('/');

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
    /// Get folder content (placeholder for FileSystemRead controller integration)
    /// </summary>
    [HttpGet("{*folderPath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetFolderContent(string folderPath = "", CancellationToken ct = default)
    {
        // This endpoint will be handled by FileSystemReadControllerUser
        // Redirect or return information about the correct endpoint
        return Ok(
            new ApiResponse<object>(
                true,
                "Use /api/user/filesystem/content endpoint for folder content retrieval",
                new { redirectTo = $"/api/user/filesystem/content/{folderPath}" }
            )
        );
    }
}

/// <summary>
/// Request model for creating folders
/// </summary>
public class CreateFolderRequest
{
    public string FolderPath { get; set; } = string.Empty;
}
