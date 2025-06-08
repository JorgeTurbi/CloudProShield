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
        public async Task<IActionResult> Download(Guid userId,
                                          string relativePath,
                                          CancellationToken ct)
        {
            var (ok, stream, contentType, reason) =
                await _storage.GetFileAsync(userId, relativePath, ct);

            if (!ok)
                return NotFound(new { error = reason });

            return File(stream!, contentType ?? "application/octet-stream");
        }

    }
}