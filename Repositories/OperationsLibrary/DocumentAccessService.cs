using System.Collections.Concurrent;
using System.Security.Cryptography;
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

    // Cache seguro en memoria para tokens de acceso
    private static readonly ConcurrentDictionary<
        string,
        SecureDocumentAccessInfo
    > _secureAccessCache = new();

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

    public async Task PrepareSecureDocumentAccessAsync(
        Guid documentId,
        Guid signerId,
        string accessToken,
        string sessionId,
        string requestFingerprint,
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

        // Generar clave única para el cache
        var cacheKey = GenerateSecureCacheKey(accessToken, sessionId, requestFingerprint);

        // Guardar información de acceso segura
        var secureAccessInfo = new SecureDocumentAccessInfo
        {
            DocumentId = documentId,
            SignerId = signerId,
            AccessToken = accessToken,
            SessionId = sessionId,
            RequestFingerprint = requestFingerprint,
            ExpiresAt = expiresAt,
            Document = document,
            CreatedAt = DateTime.UtcNow,
            // AccessCount = 0,
            // MaxAccessCount = 5, // Limitar intentos de acceso
        };

        _secureAccessCache.TryAdd(cacheKey, secureAccessInfo);

        // Limpiar entradas expiradas
        CleanExpiredAccess();

        _log.LogInformation(
            "Acceso seguro preparado para documento {DocumentId}, session {SessionId}",
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
        // Buscar en cache todas las entradas que coincidan con token y sesión
        var matchingEntries = _secureAccessCache
            .Where(kvp => kvp.Value.AccessToken == accessToken && kvp.Value.SessionId == sessionId)
            .ToList();

        if (!matchingEntries.Any())
            throw new UnauthorizedAccessException("Token inválido o expirado");

        // Usar la primera entrada válida (debería haber solo una)
        var (cacheKey, accessInfo) = matchingEntries.First();

        // Verificar expiración
        if (accessInfo.ExpiresAt < DateTime.UtcNow)
        {
            _secureAccessCache.TryRemove(cacheKey, out _);
            throw new UnauthorizedAccessException("Token expirado");
        }

        // // Verificar límite de accesos
        // if (accessInfo.AccessCount >= accessInfo.MaxAccessCount)
        // {
        //     _log.LogWarning(
        //         "Límite de accesos excedido para documento {DocumentId}",
        //         accessInfo.DocumentId
        //     );
        //     _secureAccessCache.TryRemove(cacheKey, out _);
        //     throw new UnauthorizedAccessException("Límite de accesos excedido");
        // }

        // // Incrementar contador de accesos
        // accessInfo.AccessCount++;

        // Obtener documento físico
        var space =
            await _db
                .Spaces.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == accessInfo.Document.SpaceId, ct)
            ?? throw new FileNotFoundException("Space no encontrado");

        var year = accessInfo.Document.CreateAt.Year.ToString();
        var rel = accessInfo.Document.RelativePath.Replace('\\', '/').TrimStart('/');
        var documentPath = Path.Combine(_rootPath, year, space.CustomerId.ToString("N"), rel);

        if (!File.Exists(documentPath))
            throw new FileNotFoundException($"Documento físico no encontrado: {documentPath}");

        var bytes = await File.ReadAllBytesAsync(documentPath, ct);

        _log.LogInformation(
            "Documento accedido exitosamente {DocumentId}, acceso #{AccessCount}",
            accessInfo.DocumentId
            // accessInfo.AccessCount
        );

        return new DocumentAccessResultDto
        {
            DocumentId = accessInfo.DocumentId,
            FileName = accessInfo.Document.FileName,
            ContentType = accessInfo.Document.ContentType,
            Content = bytes,
            SessionId = sessionId,
        };
    }

    public async Task<bool> ValidateAccessRequestAsync(
        string accessToken,
        string sessionId,
        string requestFingerprint,
        CancellationToken ct
    )
    {
        var cacheKey = GenerateSecureCacheKey(accessToken, sessionId, requestFingerprint);

        if (!_secureAccessCache.TryGetValue(cacheKey, out var accessInfo))
            return false;

        return accessInfo.ExpiresAt > DateTime.UtcNow;
            // && accessInfo.AccessCount < accessInfo.MaxAccessCount;
    }

    private string GenerateSecureCacheKey(
        string accessToken,
        string sessionId,
        string requestFingerprint
    )
    {
        var data = $"{accessToken}:{sessionId}:{requestFingerprint}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash);
    }

    private void CleanExpiredAccess()
    {
        var expiredKeys = _secureAccessCache
            .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _secureAccessCache.TryRemove(key, out _);
        }

        _log.LogDebug("Limpiadas {Count} entradas expiradas del cache", expiredKeys.Count);
    }
}
