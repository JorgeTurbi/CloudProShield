using System.Net.Mime;
using System.Security.Claims;
using CloudShield.Services.OperationStorage;
using Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Manages files belonging to a user
/// </summary>
[ApiController]
[Authorize]
[Route("api/user/files")]
public class FilesControllerUser : ControllerBase
{
    private readonly IStorageServiceUser _storage;

    public FilesControllerUser(IStorageServiceUser storage) => _storage = storage;

    /// <summary>
    /// Upload a file to user's storage
    /// </summary>
    [HttpPost]
    [RequestFormLimits(MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024)] // 5 GB
    [Consumes(MediaTypeNames.Multipart.FormData)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadFile(
        IFormFile file,
        [FromForm] string? folder,
        CancellationToken ct = default
    )
    {
        // Validate file
        if (file == null || file.Length == 0)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return BadRequest(new ApiResponse<object>(false, "File is empty or null", null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        try
        {
            var (success, pathOrReason) = await _storage.SaveFileAsync(userId, file, ct, folder);

            if (success)
            {
                return CreatedAtAction(
                    nameof(DownloadFile),
                    new { relativePath = pathOrReason },
                    new ApiResponse<object>(
                        true,
                        "File uploaded successfully",
                        new { path = pathOrReason }
                    )
                );
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return BadRequest(new ApiResponse<object>(false, pathOrReason, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        catch (Exception ex)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return StatusCode(
                500,
                new ApiResponse<object>(false, ex.Message.ToString(), null)
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }

    /// <summary>
    /// Download a file from user's storage
    /// </summary>
    [HttpGet("{*relativePath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadFile(
        string relativePath,
        CancellationToken ct = default
    )
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        if (string.IsNullOrWhiteSpace(relativePath))
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return BadRequest(new ApiResponse<object>(false, "File path is required", null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        try
        {
            // Decode and normalize path
            var decodedPath = Uri.UnescapeDataString(relativePath)
                .Replace('\\', '/')
                .TrimStart('/');

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

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return NotFound(new ApiResponse<object>(false, reason, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        catch (Exception ex)
        {

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return StatusCode(
                500,
                new ApiResponse<object>(false, ex.Message.ToString(), null)
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }

    /// <summary>
    /// Delete a file from user's storage
    /// </summary>
    [HttpDelete("{*relativePath}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteFile(string relativePath, CancellationToken ct = default)
    {
        // Get user ID from token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return Unauthorized(
                new ApiResponse<object>(false, "Invalid or missing user ID in token", null)
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        if (string.IsNullOrWhiteSpace(relativePath))
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return BadRequest(new ApiResponse<object>(false, "File path is required", null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        try
        {
            // Decode and normalize path exactly like in Download
            var decodedPath = Uri.UnescapeDataString(relativePath)
                .Replace('\\', '/')
                .TrimStart('/');

            // Find file metadata first
            var metadata = await _storage.FindMetaAsyncUser(userId, decodedPath, ct);
            if (metadata == null)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                return NotFound(new ApiResponse<object>(false, "File not found", null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Delete the file
            var (success, reason) = await _storage.DeleteFileAsync(
                metadata.SpaceId,
                decodedPath,
                metadata.SizeBytes,
                ct
            );

            if (success)
                return NoContent();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return BadRequest(new ApiResponse<object>(false, reason, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        catch (Exception ex)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return StatusCode(
                500,
                new ApiResponse<object>(false, ex.Message.ToString(), null)
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }
}
