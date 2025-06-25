using CloudShield.Services.OperationStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class DocumentSigningController : ControllerBase
{
    private readonly IDocumentAccessService _documentAccess;
    private readonly ILogger<DocumentSigningController> _log;

    public DocumentSigningController(
        IDocumentAccessService documentAccess,
        ILogger<DocumentSigningController> log
    )
    {
        _documentAccess = documentAccess;
        _log = log;
    }

    [HttpGet("document")]
    public async Task<IActionResult> GetDocumentForSigning(
        [FromQuery] string accessToken,
        [FromQuery] string sessionId,
        CancellationToken ct
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest("AccessToken y SessionId son requeridos");
            }

            var result = await _documentAccess.GetDocumentForSigningAsync(
                accessToken,
                sessionId,
                ct
            );

            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _log.LogWarning(ex, "Acceso no autorizado al documento");
            return Unauthorized(ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            _log.LogError(ex, "Documento no encontrado");
            return NotFound("Documento no encontrado");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error obteniendo documento para firma");
            return StatusCode(500, "Error interno del servidor");
        }
    }
}
