using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using CloudShield.Commons.Helpers;
using CloudShield.Entities.Operations;
using Commons;
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
        _rootPath = cfg["Storage:RootPath"] ?? "StorageCloud"; // configurable
        _db = db;
        _log = log;
    }

    /* ====================================================== */
    /*  MÉTODO 0 · ESTRUCTURA (re-uso)                        */
    /* ====================================================== */
    private async Task EnsureStructureAsync(
        Guid userId,
        IEnumerable<string> extraFolders,
        CancellationToken ct = default
    )
    {
        var userRoot = FileStoragePathResolver.UserRoot(_rootPath, userId);

        // Crea raíz + subcarpetas
        Directory.CreateDirectory(userRoot);
        foreach (var seg in extraFolders.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var dir = Path.Combine(
                new[] { userRoot }.Concat(seg.Split('/', StringSplitOptions.RemoveEmptyEntries)).ToArray()
            );
            Directory.CreateDirectory(dir);
        }

        // Garantiza SpaceCloud
        var space = await _db.SpacesClouds.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (space == null)
        {
            space = new SpaceCloud
            {
                UserId = userId,
                MaxBytes = 5L * 1024 * 1024 * 1024, // 5 GB por defecto
                UsedBytes = 0,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                RowVersion = Array.Empty<byte>()
            };
            await _db.SpacesClouds.AddAsync(space, ct);
            await _db.SaveChangesAsync(ct);
        }
    }

    /* ====================================================== */
    /*  MÉTODO 1 · SAVE                                       */
    /* ====================================================== */
    public async Task<(bool ok, string relativePathOrReason)> SaveFileAsync(
        Guid userId,
        IFormFile file,
        CancellationToken ct,
        string? customFolder = null
    )
    {
        // 1) SpaceCloud
        var space = await _db.SpacesClouds.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (space == null)
        {
            await EnsureStructureAsync(userId, Array.Empty<string>(), ct);
            space = await _db.SpacesClouds.FirstAsync(s => s.UserId == userId, ct);
        }

        // 2) Cuota
        if (space.UsedBytes + file.Length > space.MaxBytes)
            return (false, "Se supera la cuota asignada");

        // 3) Carpetas
        var targetPath = SanitizeFolder(customFolder ?? string.Empty);         // puede ser "", "docs", "docs/2024"
        var userRoot = FileStoragePathResolver.UserRoot(_rootPath, userId);
        var finalFolder = string.IsNullOrEmpty(targetPath)
                         ? userRoot
                         : Path.Combine(userRoot, targetPath);
        Directory.CreateDirectory(finalFolder);

        var safeName = Path.GetFileName(file.FileName);
        var fullPath = Path.Combine(finalFolder, safeName);
        var relativePath = string.IsNullOrEmpty(targetPath)
                         ? safeName
                         : $"{targetPath}/{safeName}";
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            await file.CopyToAsync(fs, ct);

        // 4) Metadatos
        var meta = new FileResourceCloud
        {
            SpaceId = space.Id,
            FileName = safeName,
            ContentType = file.ContentType ?? "application/octet-stream",
            RelativePath = relativePath,
            SizeBytes = file.Length,
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        await _db.FileResourcesCloud.AddAsync(meta, ct);

        space.UsedBytes += file.Length;
        space.UpdateAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("📥 {File} guardado para usuario {UserId}", safeName, userId);
        return (true, relativePath);
    }

    /* ====================================================== */
    /*  MÉTODO 2 · GET FILE                                   */
    /* ====================================================== */
    public async Task<(bool ok, Stream content, string contentType, string reason)> GetFileAsync(
        Guid userId,
        string relativePath,
        CancellationToken ct
    )
    {
        var space = await _db.SpacesClouds
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (space == null)
            return (false, null, null, "Espacio no encontrado")!;

        var meta = await _db.FileResourcesCloud
            .AsNoTracking()
            .FirstOrDefaultAsync(fr => fr.SpaceId == space.Id && fr.RelativePath == relativePath, ct);

        if (meta == null)
            return (false, null, null, "Archivo no registrado")!;

        var fullPath = Path.Combine(FileStoragePathResolver.UserRoot(_rootPath, userId), relativePath);
        if (!File.Exists(fullPath))
            return (false, null, null, "Archivo físico no encontrado")!;

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (true, stream, meta.ContentType, null)!;
    }

    /* ====================================================== */
    /*  MÉTODO 3 · DELETE FILE                                */
    /* ====================================================== */
    public async Task<(bool ok, string reason)> DeleteFileAsync(
        Guid spaceId,
        string relativePath,
        long fileBytes,
        CancellationToken ct
    )
    {
        var space = await _db.SpacesClouds.FirstOrDefaultAsync(s => s.Id == spaceId, ct);
        if (space == null) return (false, "Espacio no encontrado");

        var meta = await _db.FileResourcesCloud
            .FirstOrDefaultAsync(fr => fr.SpaceId == spaceId && fr.RelativePath == relativePath, ct);

        if (meta == null) return (false, "Archivo no registrado");

        var safeRel = relativePath.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.Combine(FileStoragePathResolver.UserRoot(_rootPath, space.UserId), safeRel);

        if (File.Exists(fullPath)) File.Delete(fullPath);

        space.UsedBytes = Math.Max(space.UsedBytes - fileBytes, 0);
        _db.FileResourcesCloud.Remove(meta);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("🗑️  {Rel} eliminado (usuario {Usr})", relativePath, space.UserId);
        return (true, null);
    }

    /* ====================================================== */
    /*  MÉTODO 4 · CREATE FOLDER                              */
    /* ====================================================== */
    public async Task<ApiResponse<object>> CreateFolderAsync(
        Guid userId,
        string relativePath,
        CancellationToken ct
    )
    {
        var clean = SanitizeFolder(relativePath);
        if (string.IsNullOrWhiteSpace(clean))
            return new(false, "Ruta no válida", null);

        try
        {
            await EnsureStructureAsync(userId, new[] { clean }, ct);
            _log.LogInformation("📂 Carpeta {Path} creada para usuario {UserId}", clean, userId);
            return new(true, "Creada", new { path = clean });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error creando carpeta {Path} para {UserId}", clean, userId);
            return new(false, "Error interno", null);
        }
    }

    /* ====================================================== */
    /*  MÉTODO 5 · DELETE FOLDER                              */
    /* ====================================================== */
    public async Task<(bool ok, string reason)> DeleteFolderAsync(
        Guid userId,
        string folder,
        CancellationToken ct
    )
    {
        folder = SanitizeFolder(folder);
        if (string.IsNullOrWhiteSpace(folder))
            return (false, "Nombre de carpeta no válido");

        var space = await _db.SpacesClouds
            .Include(s => s.FileResourcesCloud)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (space == null) return (false, "Espacio no encontrado");

        var toDelete = space.FileResourcesCloud
            .Where(f => f.RelativePath.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        _db.FileResourcesCloud.RemoveRange(toDelete);
        space.UsedBytes = Math.Max(space.UsedBytes - toDelete.Sum(f => f.SizeBytes), 0);

        var dirPath = Path.Combine(FileStoragePathResolver.UserRoot(_rootPath, userId), folder);
        if (Directory.Exists(dirPath)) Directory.Delete(dirPath, true);

        await _db.SaveChangesAsync(ct);
        _log.LogInformation("🗑️  Carpeta {Folder} eliminada (usuario {Usr})", folder, userId);
        return (true, null);
    }

    /* ====================================================== */
    /*  MÉTODO 6 · ZIP FOLDER                                 */
    /* ====================================================== */
    public async Task<(bool ok, Stream content, string reason)> GetFolderZipAsync(
        Guid userId,
        string folder,
        CancellationToken ct
    )
    {
        folder = SanitizeFolder(folder);
        if (string.IsNullOrWhiteSpace(folder))
            return (false, null, "Nombre de carpeta no válido");

        var space = await _db.SpacesClouds
            .Include(s => s.FileResourcesCloud)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (space == null) return (false, null, "Espacio no encontrado");

        var items = space.FileResourcesCloud
            .Where(f => f.RelativePath.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!items.Any()) return (false, null, "Carpeta vacía o inexistente");

        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var meta in items)
            {
                var full = Path.Combine(FileStoragePathResolver.UserRoot(_rootPath, userId), meta.RelativePath);
                if (File.Exists(full))
                    zip.CreateEntryFromFile(full, meta.RelativePath.Replace('\\', '/'), CompressionLevel.Fastest);
            }
        }
        ms.Position = 0;
        return (true, ms, null);
    }

    /* ====================================================== */
    /*  MÉTODO 7 · FIND META                                  */
    /* ====================================================== */
    public async Task<FileResourceCloud?> FindMetaAsyncUser(
        Guid userId,
        string relativePath,
        CancellationToken ct
    )
    {
        var space = await _db.SpacesClouds.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (space == null) return null;
        return await _db.FileResourcesCloud
            .AsNoTracking()
            .FirstOrDefaultAsync(fr => fr.SpaceId == space.Id && fr.RelativePath == relativePath, ct);
    }

    /* ====================================================== */
    /*  MÉTODO 8 · CreateFolderZipAsync (para link directo)    */
    /* ====================================================== */
    public async Task<byte[]> CreateFolderZipAsync(Guid userId, string folder, CancellationToken ct = default)
    {
        var folderSanitized = SanitizeFolder(folder);
        var physical = Path.Combine(FileStoragePathResolver.UserRoot(_rootPath, userId), folderSanitized);

        if (!Directory.Exists(physical)) return null!;

        await using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var file in Directory.GetFiles(physical, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(physical, file);
                zip.CreateEntryFromFile(file, rel, CompressionLevel.Fastest);
            }
        }
        return ms.ToArray();
    }

    /* ====================================================== */
    /*  Helpers                                               */
    /* ====================================================== */
    private static string GetCategory(string ext, string mime)
    {
        ext = ext.ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "images",
            ".mp4" or ".mkv" or ".mov" or ".avi" => "videos",
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".pptx" => "docs",
            ".zip" or ".rar" or ".7z" or ".tar" => "archives",
            _ when mime.StartsWith("image/") => "images",
            _ when mime.StartsWith("video/") => "videos",
            _ => "others"
        };
    }

    private static string SanitizeFolder(string path) =>
        Regex.Replace(path ?? "", @"[^A-Za-z0-9_\- /]", "")
              .Replace('\\', '/')
              .Replace("//", "/")
              .Trim('/', ' ');
}
