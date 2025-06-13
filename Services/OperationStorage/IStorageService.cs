using CloudShield.Entities.Operations;

namespace CloudShield.Services.OperationStorage;

public interface IStorageService
{
    Task<(bool ok, string relativePathOrReason)> SaveFileAsync(
        Guid customerId,
        IFormFile file,
        CancellationToken ct,  string? customFolder  
    );
    Task<(bool ok, string reason)> DeleteFileAsync(
        Guid spaceId,
        string relativePath,
        long fileBytes,
        CancellationToken ct
    );
    Task<(bool ok, Stream content, string contentType, string reason)> GetFileAsync(
        Guid customerId,
        string relativePath,
        CancellationToken ct
    );

    // …otros métodos (descarga, listado, etc.)
    /* METADATO ← NUEVO */
    Task<FileResource> FindMetaAsync(Guid customerId, string relativePath, CancellationToken ct);
}
