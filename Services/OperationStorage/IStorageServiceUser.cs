using CloudShield.Entities.Operations;
using Commons;

namespace CloudShield.Services.OperationStorage;

public interface IStorageServiceUser
{
    Task<(bool ok, string relativePathOrReason)> SaveFileAsync(
        Guid UserId,
        IFormFile file,
        CancellationToken ct
    );
    Task<(bool ok, string reason)> DeleteFileAsync(
        Guid spaceId,
        string relativePath,
        long fileBytes,
        CancellationToken ct
    );
    Task<(bool ok, Stream content, string contentType, string reason)> GetFileAsync(
        Guid UserId,
        string relativePath,
        CancellationToken ct
    );

    // …otros métodos (descarga, listado, etc.)
    /* METADATO ← NUEVO */
    Task<FileResource> FindMetaAsync(Guid UserId, string relativePath, CancellationToken ct);
    Task<byte[]> CreateFolderZipAsync(Guid customerId, string relativePath, CancellationToken ct);
}
