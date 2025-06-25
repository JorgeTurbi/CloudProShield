using System.Collections.Concurrent;
using CloudShield.Services.OperationStorage;
using DataContext;
using DTOs.DocumentAccess;
using Microsoft.EntityFrameworkCore;

namespace Reponsitories.OperationsLibrary;

public class DocumentAccessService : IDocumentAccessService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DocumentAccessService> _log;
    private readonly string _rootPath;

    // Cache temporal en memoria para tokens de acceso
    private static readonly ConcurrentDictionary<string, DocumentAccessInfo> _accessCache = new();

    public DocumentAccessService(
        ApplicationDbContext db,
        ILogger<DocumentAccessService> log,
        IConfiguration cfg
    )
    {
        _db = db;
        _log = log;
        _rootPath = cfg["Storage:RootPath"] ?? "storage";
    }

    public async Task PrepareDocumentAccessAsync(
        Guid documentId,
        Guid signerId,
        string accessToken,
        string sessionId,
        DateTime expiresAt,
        CancellationToken ct
    )
    {
        // Verificar que el documento existe
        var document = await _db
            .FileResources.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == documentId, ct);

        if (document == null)
        {
            _log.LogWarning("Documento {DocumentId} no encontrado", documentId);
            return;
        }

        // Guardar información de acceso temporal
        var accessInfo = new DocumentAccessInfo
        {
            DocumentId = documentId,
            SignerId = signerId,
            AccessToken = accessToken,
            SessionId = sessionId,
            ExpiresAt = expiresAt,
            Document = document,
        };

        _accessCache.TryAdd($"{accessToken}:{sessionId}", accessInfo);

        // Limpiar entradas expiradas (housekeeping)
        CleanExpiredAccess();

        _log.LogInformation(
            "Acceso preparado para documento {DocumentId}, session {SessionId}",
            documentId,
            sessionId
        );
    }

    public async Task<DocumentAccessResultDto> GetDocumentForSigningAsync(
        string accessToken,
        string sessionId,
        CancellationToken ct
    )
    {
        var key = $"{accessToken}:{sessionId}";

        if (!_accessCache.TryGetValue(key, out var accessInfo))
            throw new UnauthorizedAccessException("Token inválido o expirado");

        if (accessInfo.ExpiresAt < DateTime.UtcNow)
        {
            _accessCache.TryRemove(key, out _);
            throw new UnauthorizedAccessException("Token expirado");
        }

        /* ► armamos ruta física correcta */
        var space =
            await _db
                .Spaces.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == accessInfo.Document.SpaceId, ct)
            ?? throw new FileNotFoundException("Space no encontrado");

        var year = accessInfo.Document.CreateAt.Year.ToString();
        var root = _rootPath; // “StorageCloud”, etc.
        var rel = accessInfo.Document.RelativePath.Replace('\\', '/').TrimStart('/');

        var documentPath = Path.Combine(root, year, space.CustomerId.ToString("N"), rel);

        if (!File.Exists(documentPath))
            throw new FileNotFoundException($"Documento físico no encontrado: {documentPath}");

        var bytes = await File.ReadAllBytesAsync(documentPath, ct);

        return new DocumentAccessResultDto
        {
            DocumentId = accessInfo.DocumentId,
            FileName = accessInfo.Document.FileName,
            ContentType = accessInfo.Document.ContentType,
            Content = bytes,
            SessionId = sessionId,
        };
    }

    private void CleanExpiredAccess()
    {
        var expiredKeys = _accessCache
            .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _accessCache.TryRemove(key, out _);
        }
    }
}
