using CloudShield.Commons.Helpers;
using CloudShield.Entities.Operations;
using DataContext;
using Microsoft.EntityFrameworkCore;

namespace CloudShield.Services.OperationStorage;

public class LocalDiskStorageServiceUser : IStorageServiceUser
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<LocalDiskStorageServiceUser> _log;
    private readonly string _rootPath;

    public LocalDiskStorageServiceUser(
        IConfiguration cfg,
        ApplicationDbContext db,
        ILogger<LocalDiskStorageServiceUser> log
    )
    {
        _rootPath = cfg["Storage:RootPath"] ?? "storage"; // configurable
        _db = db;
        _log = log;
    }

    /* ----------------------------------------------------------- */
    /*  MÉTODO 1: Guardar y segmentar                              */
    /* ----------------------------------------------------------- */
    public async Task<(bool ok, string relativePathOrReason)> SaveFileAsync(
        Guid userId,
        IFormFile file,
        CancellationToken ct
    )
    {
        // 1) Obtiene (o crea) el espacio
        var space = await _db
            .SpacesClouds.AsTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (space is null)
        {
            space = new SpaceCloud
            {
                UserId = userId,
                MaxBytes = 5L * 1024 * 1024 * 1024, // 5 GB
                UsedBytes = 0,
                CreateAt = DateTime.UtcNow
            };
            await _db.SpacesClouds.AddAsync(space, ct);
            await _db.SaveChangesAsync(ct);
        }

        // 2) Cuota
        if (space.UsedBytes + file.Length > space.MaxBytes)
            return (false, "Se supera la cuota asignada");

        // 3) Segmentación por tipo
        var category = GetCategory(Path.GetExtension(file.FileName), file.ContentType);
        var userFolder = FileStoragePathResolver.UserRoot(_rootPath, userId);
        Directory.CreateDirectory(userFolder);

        var categoryFolder = Path.Combine(userFolder, category);
        Directory.CreateDirectory(categoryFolder);

        var safeFileName = Path.GetFileName(file.FileName);
        var filePath = Path.Combine(categoryFolder, safeFileName);
        var relativePath = $"{category}/{safeFileName}";

        // 4) Guarda en disco
        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(stream, ct);
        }

        // 5) Guarda metadatos
        var meta = new FileResourceCloud
        {
            SpaceId = space.Id,
            FileName = safeFileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            RelativePath = relativePath,
            SizeBytes = file.Length,
            CreateAt = DateTime.UtcNow,
        };
        await _db.FileResourcesCloud.AddAsync(meta, ct);

        space.UsedBytes += file.Length;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "Archivo {File} ({Cat}) guardado para {User}",
            safeFileName,
            category,
            userId
        );

        return (true, relativePath);
    }

    /* ----------------------------------------------------------- */
    /*  MÉTODO 2: Obtener archivo por path                         */
    /* ----------------------------------------------------------- */
    public async Task<(bool ok, Stream content, string contentType, string reason)> GetFileAsync(
        Guid userId,
        string relativePath,
        CancellationToken ct
    )
    {
        var space = await _db
            .SpacesClouds.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (space is null)
            return (false, null, null, "Espacio no encontrado")!;

        // Verifica que el archivo pertenezca al cliente
        var meta = await _db
            .FileResourcesCloud.AsNoTracking()
            .Where(fr => fr.SpaceId == space.Id && fr.RelativePath == relativePath)
            .FirstOrDefaultAsync(ct);
        if (meta is null)
            return (false, null, null, "Archivo no registrado")!;

        var fullPath = Path.Combine(FileStoragePathResolver.UserRoot(_rootPath, userId), relativePath);
        if (!System.IO.File.Exists(fullPath))
            return (false, null, null, "Archivo físico no encontrado")!;

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (true, stream, meta.ContentType, null)!;
    }

    /* ----------------------------------------------------------- */
    /*  Utilitario: categorizar                                    */
    /* ----------------------------------------------------------- */
    private static string GetCategory(string extension, string contentType)
    {
        extension = extension.ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "images",
            ".mp4" or ".mkv" or ".mov" or ".avi" => "videos",
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".pptx" => "docs",
            ".zip" or ".rar" or ".7z" or ".tar" => "archives",
            _ when contentType?.StartsWith("image/") == true => "images",
            _ when contentType?.StartsWith("video/") == true => "videos",
            _ => "others",
        };
    }

    public async Task<(bool ok, string reason)> DeleteFileAsync(
        Guid spaceId,
        string relativePath,
        long fileBytes,
        CancellationToken ct
    )
    {
        // 1) Obtiene el espacio
        var space = await _db.SpacesClouds.AsTracking().FirstOrDefaultAsync(s => s.Id == spaceId, ct);

        if (space is null)
            return (false, "Espacio no encontrado");

        // 2) Busca el metadato del archivo
        var meta = await _db.FileResourcesCloud.FirstOrDefaultAsync(
            fr => fr.SpaceId == spaceId && fr.RelativePath == relativePath,
            ct
        );

        if (meta is null)
            return (false, "Archivo no registrado");

        // 3) Construye la ruta física (evita traversal)
        var safeRelativePath = relativePath.Replace('\\', '/').TrimStart('/').Trim();
        var fullPath = Path.Combine(FileStoragePathResolver.UserRoot(_rootPath, space.UserId), relativePath);
        var normalizedRoot = Path.GetFullPath(
            Path.Combine(_rootPath, space.UserId.ToString("N"))
        );
        var normalizedPath = Path.GetFullPath(fullPath);

        if (!normalizedPath.StartsWith(normalizedRoot)) // intento de salir de la carpeta
            return (false, "Ruta no válida");

        // 4) Borra el archivo físico
        if (File.Exists(normalizedPath))
            File.Delete(normalizedPath);

        // 5) Actualiza UsedBytes y borra metadato
        space.UsedBytes = Math.Max(space.UsedBytes - fileBytes, 0);
        _db.FileResourcesCloud.Remove(meta);

        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "Archivo {RelPath} eliminado de Space {SpaceId}",
            relativePath,
            spaceId
        );
        return (true, null)!;
    }

#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    public async Task<FileResourceCloud?> FindMetaAsync(
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
        Guid userId,
        string relativePath,
        CancellationToken ct
    )
    {
        var space = await _db
            .SpacesClouds.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == userId, ct);
        if (space is null)
        {
            _log.LogWarning("No se encontró el espacio para el cliente {UserId}", userId);
            return null!;
        }

        return await _db
            .FileResourcesCloud.AsNoTracking()
            .Where(fr => fr.SpaceId == space.Id && fr.RelativePath == relativePath)
            .FirstOrDefaultAsync(ct);
    }
}
