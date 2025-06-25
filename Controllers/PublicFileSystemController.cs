using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CloudShield.DTOs.FileSystem;
using CloudShield.Services.FileSystemServices;
using Commons;
using Commons.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("filesystem")]
public class PublicFileSystemController : ControllerBase
{
    private readonly IFileSystemReadService _fileSystemService;
    private readonly ILogger<PublicFileSystemController> _logger;
    private readonly JwtSettingsNew _cfg;


    public PublicFileSystemController(IFileSystemReadService fileSystemService, ILogger<PublicFileSystemController> logger, IOptions<JwtSettingsNew> opt)
    {
        _fileSystemService = fileSystemService;
        _logger = logger;
        _cfg = opt.Value;
    }

    [HttpGet("files/{documentId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<FileItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FileItemDTO>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<FileItemDTO>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFileById(Guid documentId, [FromQuery] string? token= null, CancellationToken ct = default)
    {
        try
        {


            _logger.LogInformation("Solicitando archivo con ID {DocumentId}", documentId);
            var result = await _fileSystemService.GetFileByIdAsync(documentId, ct);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener archivo con ID {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<FileItemDTO>(false, "Error interno del servidor", null));
        }
    }
   

}
