using Microsoft.AspNetCore.Mvc;
using CloudShield.Services.OperationStorage;   // Donde vive tu IStorageService
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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


    [HttpPost()]                          // ← el id va en la ruta          // 5 GB (ajusta)
    [RequestFormLimits(MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024)]
    [Consumes(MediaTypeNames.Multipart.FormData)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(                   // ↙ en la URL
         IFormFile file,                           // ↙ se muestra como file-picker
        CancellationToken ct)
    {
        string? Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(Id, out Guid userId))
        {
            // El GUID es válido, puedes usar la variable 'guid' aquí
        }
        else
        {
            // El string no es un GUID válido
            Console.WriteLine("El formato del GUID no es válido.");
        }


        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Archivo vacío o no enviado." });

        var (ok, relativePathOrReason) =
            await _storage.SaveFileAsync(userId, file, ct);

        if (!ok)
            return BadRequest(new { error = relativePathOrReason });

        return CreatedAtAction(
            nameof(Download),                                // GET de descarga
            new { userId, relativePath = relativePathOrReason },
            new { path = relativePathOrReason });
    }


    [HttpDelete("{*relativePath}")]
    public async Task<IActionResult> Delete(
      
        string relativePath,
        CancellationToken ct)
    {

  string? Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (Guid.TryParse(Id, out Guid userId))
      {
        // El GUID es válido, puedes usar la variable 'guid' aquí
      }
      else
      {
        // El string no es un GUID válido
        Console.WriteLine("El formato del GUID no es válido.");
      }

        if (string.IsNullOrWhiteSpace(relativePath))
            return BadRequest(new { error = "relativePath requerido." });

        // Recupera el tamaño registrado para ajustar UsedBytes:
        var meta = await _storage.FindMetaAsync(userId, relativePath, ct);
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
      
        string relativePath,
        CancellationToken ct)
    {
          string? Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (Guid.TryParse(Id, out Guid userId))
      {
        // El GUID es válido, puedes usar la variable 'guid' aquí
      }
      else
      {
        // El string no es un GUID válido
        Console.WriteLine("El formato del GUID no es válido.");
      }
        // 1) Decodificamos %2F, espacios, tildes, etc.
        var decodedPath = Uri.UnescapeDataString(relativePath);

        // 2) (Opcional) Normaliza para evitar traversal
        decodedPath = decodedPath.Replace('\\', '/').TrimStart('/');
        var (ok, stream, contentType, reason) =
            await _storage.GetFileAsync(userId, decodedPath, ct);

        if (!ok)
            return NotFound(new { error = reason });

        return File(stream!, contentType ?? MediaTypeNames.Application.Octet);
    }
}
