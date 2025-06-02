using CloudShield.Services.OperationStorage;
using Microsoft.AspNetCore.Mvc;

namespace CloudProShield.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperationController : ControllerBase
    {

        private readonly IStorageService _storage;
        public OperationController(IStorageService storage)
        {
            _storage = storage;
        }


        [HttpGet("{*relativePath}")]
        public async Task<IActionResult> Download(Guid customerId,
                                          string relativePath,
                                          CancellationToken ct)
        {
            var (ok, stream, contentType, reason) =
                await _storage.GetFileAsync(customerId, relativePath, ct);

            if (!ok)
                return NotFound(new { error = reason });

            return File(stream!, contentType ?? "application/octet-stream");
        }

    }
}