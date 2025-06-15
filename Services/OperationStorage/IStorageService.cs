using CloudShield.Entities.Operations;
using Commons;

namespace CloudShield.Services.OperationStorage;

public interface IStorageService
{
    Task<(bool ok, string relativePathOrReason)> SaveFileAsync(
        Guid customerId,
        IFormFile file,
        CancellationToken ct,
        string? customFolder
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
    Task<ApiResponse<object>> CreateFolderAsync(
        Guid customerId,
        string relativePath,
        CancellationToken ct
    );

    /// <summary>Elimina una carpeta completa (salvo nativas).</summary>
    Task<(bool ok, string reason)> DeleteFolderAsync(
        Guid customerId,
        string folder,
        CancellationToken ct = default
    );
    Task<(bool ok, Stream content, string reason)> GetFolderZipAsync(
        Guid customerId,
        string folder,
        CancellationToken ct
    );
    Task<FileResource> FindMetaAsync(Guid customerId, string relativePath, CancellationToken ct);
}
