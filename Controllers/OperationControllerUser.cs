using System.Security.Claims;
using CloudShield.Services.OperationStorage;
using Microsoft.AspNetCore.Mvc;

namespace CloudProShield.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperationControllerUser : ControllerBase
    {

        private readonly IStorageServiceUser _storage;
        public OperationControllerUser(IStorageServiceUser storage)
        {
            _storage = storage;
        }


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
            var (ok, stream, contentType, reason) =
                await _storage.GetFileAsync(userId, relativePath, ct);

            if (!ok)
                return NotFound(new { error = reason });

            return File(stream!, contentType ?? "application/octet-stream");
        }

    }
}