using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using CloudShield.Services.OperationStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudProShield.Controllers;

[ApiController]
[Route("api/OperationControllerUser")]
[Authorize]
public sealed class OperationControllerUser : ControllerBase
{
    private readonly IStorageServiceUser _store;
    public OperationControllerUser(IStorageServiceUser store) => _store = store;

    /* ---------------------------------------------------------- */
    /*  GET api/OperationControllerUser/{*relativePath}           */
    /* ---------------------------------------------------------- */
    [HttpGet("{*relativePath}")]
    public async Task<IActionResult> Download(string relativePath, CancellationToken ct)
    {
        // 1) ID del usuario desde el token
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        // 2) Decodificar y normalizar
        var decoded = WebUtility.UrlDecode(relativePath)        // “Documents/Doc … .docx”
                       .Replace('\\', '/')
                       .TrimStart('/');

        // 3) Buscar archivo
        var (ok, stream, contentType, reason) = await _store.GetFileAsync(userId, decoded, ct);
        return ok ? File(stream!, contentType ?? MediaTypeNames.Application.Octet)
                   : NotFound(new { error = reason });
    }

    /* ---------------------------------------------------------- */
    /*  GET api/OperationControllerUser/download-folder           */
    /*     ?folder=Documents/Factura                              */
    /* ---------------------------------------------------------- */
    [HttpGet("download-folder")]
    public async Task<IActionResult> DownloadFolder([FromQuery] string folder, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var decoded = WebUtility.UrlDecode(folder).Trim('/');   // misma higiene

        var bytes = await _store.CreateFolderZipAsync(userId, decoded, ct);
        if (bytes == null) return NotFound("La carpeta no existe");

        return File(bytes, "application/zip", $"{Path.GetFileName(decoded)}.zip");
    }
}
