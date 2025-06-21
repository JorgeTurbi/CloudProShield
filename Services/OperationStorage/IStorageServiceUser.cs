using CloudShield.Entities.Operations;
using Commons;

namespace CloudShield.Services.OperationStorage;

public interface IStorageServiceUser
{
    /* CRUD archivos ---------------------------------------------------- */
    Task<(bool ok, string relativePathOrReason)> SaveFileAsync(
        Guid userId,
        IFormFile file,
        CancellationToken ct,
        string? customFolder = null
    );

    Task<(bool ok, string reason)> DeleteFileAsync(
        Guid spaceId,
        string relativePath,
        long fileBytes,
        CancellationToken ct
    );

    Task<(bool ok, Stream content, string contentType, string reason)> GetFileAsync(
        Guid userId,
        string relativePath,
        CancellationToken ct
    );

    /* Carpetas --------------------------------------------------------- */
    Task<ApiResponse<object>> CreateFolderAsync(
        Guid userId,
        string relativePath,
        CancellationToken ct
    );

    Task<(bool ok, string reason)> DeleteFolderAsync(
        Guid userId,
        string folder,
        CancellationToken ct
    );

    Task<(bool ok, Stream content, string reason)> GetFolderZipAsync(
        Guid userId,
        string folder,
        CancellationToken ct
    );

    /* Metadatos -------------------------------------------------------- */
    Task<FileResourceCloud?> FindMetaAsyncUser(
        Guid userId,
        string relativePath,
        CancellationToken ct
    );

    Task<byte[]> CreateFolderZipAsync(Guid customerId, string relativePath, CancellationToken ct);
}
