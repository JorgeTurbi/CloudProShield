using Microsoft.AspNetCore.Mvc;
using CloudShield.Services.OperationStorage;   // Donde vive tu IStorageService
using System.Net.Mime;
using DTOs.FilesDTO;
using Microsoft.AspNetCore.Authorization;

namespace Controllers;

/// <summary>
/// Gestiona los archivos pertenecientes a un cliente (customer).
/// </summary>
[ApiController]
[Route("api/customers/files")]
public class FilesController : ControllerBase
{
    private readonly IStorageService _storage;

    public FilesController(IStorageService storage) => _storage = storage;

 [AllowAnonymous]
[HttpPost]
[RequestSizeLimit(5L * 1024 * 1024 * 1024)] // 5 GB
[RequestFormLimits(MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024)]
[Consumes(MediaTypeNames.Multipart.FormData)]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Upload(
           // Puedes usar [FromRoute] si viene en la URL
    [FromForm] FilePost file,            // obligatorio para multipart/form-data
       // viene del form como texto
    CancellationToken ct)
{
    if (file.File == null || file.File.Length == 0)
        return BadRequest(new { error = "Archivo vacío o no enviado." });

    var (ok, relativePathOrReason) =
        await _storage.SaveFileAsync(file.CustomerId, file.File, ct, file.CustomFolder); // ✅ incluye la carpeta

    if (!ok)
        return BadRequest(new { error = relativePathOrReason });

    return CreatedAtAction(
        nameof(Download),
        new { file.CustomerId, relativePath = relativePathOrReason },
        new { path = relativePathOrReason }
    );
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
         // 1) Decodificamos %2F, espacios, tildes, etc.
    var decodedPath = Uri.UnescapeDataString(relativePath);

    // 2) (Opcional) Normaliza para evitar traversal
    decodedPath = decodedPath.Replace('\\', '/').TrimStart('/');
        var (ok, stream, contentType, reason) =
            await _storage.GetFileAsync(customerId, decodedPath, ct);

        if (!ok)
            return NotFound(new { error = reason });

        return File(stream!, contentType ?? MediaTypeNames.Application.Octet);
    }
}
