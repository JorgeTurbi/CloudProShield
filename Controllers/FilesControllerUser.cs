using System.Net.Mime;
using System.Security.Claims;
using CloudShield.Services.OperationStorage; // Donde vive tu IStorageService
using Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Gestiona los archivos pertenecientes a un usuario (customer).
/// </summary>
[ApiController]
[Authorize]
[Route("api/usuario/files")]
public class FilesControllerUser : ControllerBase
{
    private readonly IStorageServiceUser _storage;

    public FilesControllerUser(IStorageServiceUser storage) => _storage = storage;

    [HttpPost()] // ← el id va en la ruta          // 5 GB (ajusta)
    [RequestFormLimits(MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024)]
    [Consumes(MediaTypeNames.Multipart.FormData)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? folder, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("Archivo vacío");

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var (ok, pathOrReason) = await _storage.SaveFileAsync(userId, file, ct, folder);
        return ok
            ? CreatedAtAction(nameof(Download), new { relativePath = pathOrReason }, new { path = pathOrReason })
            : BadRequest(pathOrReason);
    }


    [HttpDelete("{*relativePath}")]
    public async Task<IActionResult> Delete(string relativePath, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new ApiResponse<string>(false, "Token sin ID", null!));

        /* 1️⃣ Decodificar y normalizar exactamente igual que en Download */
        var decoded = Uri.UnescapeDataString(relativePath)
                         .Replace('\\', '/')
                         .TrimStart('/');

        /* 2️⃣ Buscar el metadato */
        var meta = await _storage.FindMetaAsyncUser(userId, decoded, ct);
        if (meta == null)
            return NotFound(new ApiResponse<string>(false, "Archivo no encontrado", null!));

        /* 3️⃣ Eliminar */
        var (ok, reason) = await _storage.DeleteFileAsync(meta.SpaceId, decoded, meta.SizeBytes, ct);
        return ok
            ? NoContent()
            : BadRequest(new ApiResponse<string>(false, reason, null!));
    }

    /* -------------------------------------------------------- */
    /*  GET: Descargar (opcional, útil para pruebas rápidas)    */
    /* -------------------------------------------------------- */
    [HttpGet("{*relativePath}")]
    public async Task<IActionResult> Download(string relativePath, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var decoded = Uri.UnescapeDataString(relativePath).Replace('\\', '/').TrimStart('/');
        var (ok, s, ctType, reason) = await _storage.GetFileAsync(userId, decoded, ct);

        return ok ? File(s!, ctType ?? MediaTypeNames.Application.Octet) : NotFound(reason);
    }

}
