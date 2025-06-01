using Microsoft.AspNetCore.Mvc;
using CloudShield.Services.OperationStorage;   // Donde vive tu IStorageService
using System.Net.Mime;

namespace Controllers;

/// <summary>
/// Gestiona los archivos pertenecientes a un cliente (customer).
/// </summary>
[ApiController]
[Route("api/customers/{customerId:guid}/files")]
public class FilesController : ControllerBase
{
    private readonly IStorageService _storage;

    public FilesController(IStorageService storage) => _storage = storage;

 
[HttpPost()]                          // ← el id va en la ruta
[RequestSizeLimit(5L * 1024 * 1024 * 1024)]              // 5 GB (ajusta)
[RequestFormLimits(MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024)]
[Consumes(MediaTypeNames.Multipart.FormData)]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Upload(
    Guid customerId,                         // ↙ en la URL
     IFormFile file,                           // ↙ se muestra como file-picker
    CancellationToken ct)
{
    if (file == null || file.Length == 0)
        return BadRequest(new { error = "Archivo vacío o no enviado." });

    var (ok, relativePathOrReason) =
        await _storage.SaveFileAsync(customerId, file, ct);

    if (!ok)
        return BadRequest(new { error = relativePathOrReason });

    return CreatedAtAction(
        nameof(Download),                                // GET de descarga
        new { customerId, relativePath = relativePathOrReason },
        new { path = relativePathOrReason });
}


    [HttpDelete("{*relativePath}")]
    public async Task<IActionResult> Delete(
        Guid customerId,
        string relativePath,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return BadRequest(new { error = "relativePath requerido." });

        // Recupera el tamaño registrado para ajustar UsedBytes:
        var meta = await _storage.FindMetaAsync(customerId, relativePath, ct);
        if (meta is null)
            return NotFound(new { error = "Archivo no encontrado." });

        var (ok, reason) =
            await _storage.DeleteFileAsync(meta.SpaceId, relativePath, meta.SizeBytes, ct);

        return ok
            ? NoContent()
            : BadRequest(new { error = reason });
    }

    /* -------------------------------------------------------- */
    /*  GET: Descargar (opcional, útil para pruebas rápidas)    */
    /* -------------------------------------------------------- */
    [HttpGet("{*relativePath}")]
    public async Task<IActionResult> Download(
        Guid customerId,
        string relativePath,
        CancellationToken ct)
    {
        var (ok, stream, contentType, reason) =
            await _storage.GetFileAsync(customerId, relativePath, ct);

        if (!ok)
            return NotFound(new { error = reason });

        return File(stream!, contentType ?? MediaTypeNames.Application.Octet);
    }
}
