using System.Linq;
using System.Text.RegularExpressions;
using CloudShield.Entities.Operations;
using Commons;
using DataContext;
using Microsoft.EntityFrameworkCore;

namespace CloudShield.Services.OperationStorage;

public class LocalDiskStorageService : IStorageService, IFolderProvisioner
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<LocalDiskStorageService> _log;
    private readonly string _rootPath;

    public LocalDiskStorageService(
        IConfiguration cfg,
        ApplicationDbContext db,
        ILogger<LocalDiskStorageService> log
    )
    {
        _rootPath = cfg["Storage:RootPath"] ?? "storage"; // configurable
        _db = db;
        _log = log;
    }

    /* ----------------------------------------------------------- */
    /*  MÉTODO 0: Crear Folders                             */
    /* ----------------------------------------------------------- */
    public async Task<ApiResponse<object>> CreateFolderAsync(
        Guid customerId,
        string folder,
        CancellationToken ct = default
    )
    {
        // 🔐 normaliza nombre
        folder = Regex.Replace(folder, @"[^A-Za-z0-9_\- ]", "").Trim();
        if (string.IsNullOrWhiteSpace(folder))
            return new ApiResponse<object>(false, "Nombre de carpeta no válido", null);

        try
        {
            await EnsureStructureAsync(customerId, new[] { folder }, ct);
            _log.LogInformation("Carpeta {Folder} creada para {Customer}", folder, customerId);

            return new ApiResponse<object>(true, "Carpeta creada", new { folder });
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error al crear carpeta {Folder} para {Customer}",
                folder,
                customerId
            );
            return new ApiResponse<object>(false, "Error interno al crear carpeta", null);
        }
    }

    /* ----------------------------------------------------------- */
    /*  MÉTODO 1: Guardar y segmentar                              */
    /* ----------------------------------------------------------- */
    public async Task<(bool ok, string relativePathOrReason)> SaveFileAsync(
        Guid customerId,
        IFormFile file,
        CancellationToken ct,
        string? customFolder // ✅ parámetro opcional
    )
    {
        /* ------------ 1. Obtiene o crea Space ------------------- */
        var space = await _db
            .Spaces.AsTracking()
            .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);

        if (space is null)
        {
            space = new Space
            {
                CustomerId = customerId,
                MaxBytes = 5L * 1024 * 1024 * 1024, // 5 GB
                UsedBytes = 0,
                CreateAt = DateTime.UtcNow,
            };
            await _db.Spaces.AddAsync(space, ct);
            await _db.SaveChangesAsync(ct);
        }

        /* ------------ 2. Verifica cuota ------------------------- */
        if (space.UsedBytes + file.Length > space.MaxBytes)
            return (false, "Se supera la cuota asignada");

        /* ------------ 3. Resuelve carpetas ---------------------- */
        var yearFolder = Path.Combine(_rootPath, DateTime.UtcNow.Year.ToString());
        var safeFileName = Path.GetFileName(file.FileName);
        var customerFolder = Path.Combine(yearFolder, customerId.ToString("N"));

        // 3-a) Mime normalizado (evita null en GetCategory)
        var ctMime = file.ContentType ?? "application/octet-stream";

        // 3-b) Elegir sub-carpeta + hardening
        string subFolder;
        if (!string.IsNullOrWhiteSpace(customFolder))
        {
            // 🔐 Hardening → solo letras, números, guion, underscore, espacio
            subFolder = Regex.Replace(customFolder, @"[^A-Za-z0-9_\- ]", "").Trim();
            if (string.IsNullOrWhiteSpace(subFolder))
                subFolder = "Others";
        }
        else
        {
            subFolder = GetCategory(Path.GetExtension(file.FileName), ctMime);
        }

        var finalFolder = Path.Combine(customerFolder, subFolder);
        Directory.CreateDirectory(finalFolder);

        /* ------------ 4. Copia física --------------------------- */
        var filePath = Path.Combine(finalFolder, safeFileName);
        var relativePath = Path.Combine(subFolder, safeFileName).Replace('\\', '/');

        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(stream, ct);
        }

        /* ------------ 5. Registra metadatos --------------------- */
        var now = DateTime.UtcNow;
        var meta = new FileResource
        {
            SpaceId = space.Id,
            FileName = safeFileName,
            ContentType = ctMime,
            RelativePath = relativePath,
            SizeBytes = file.Length,
            CreateAt = now,
            UpdateAt = now, // 🆕 si luego sobrescribes, actualiza aquí
        };
        await _db.FileResources.AddAsync(meta, ct);

        space.UsedBytes += file.Length;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "Archivo {File} guardado para {Customer} en carpeta {Folder}",
            safeFileName,
            customerId,
            subFolder
        );

        return (true, relativePath);
    }

    /* ----------------------------------------------------------- */
    /*  MÉTODO 2: Obtener archivo por path                         */
    /* ----------------------------------------------------------- */
    public async Task<(bool ok, Stream content, string contentType, string reason)> GetFileAsync(
        Guid customerId,
        string relativePath,
        CancellationToken ct
    )
    {
        var space = await _db
            .Spaces.AsNoTracking()
            .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);

        if (space is null)
            return (false, null, null, "Espacio no encontrado")!;

        // Verifica que el archivo pertenezca al cliente
        var meta = await _db
            .FileResources.AsNoTracking()
            .Where(fr => fr.SpaceId == space.Id && fr.RelativePath == relativePath)
            .FirstOrDefaultAsync(ct);
        if (meta is null)
            return (false, null, null, "Archivo no registrado")!;

        var fullPath = Path.Combine(_rootPath, customerId.ToString("N"), relativePath);
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
        var space = await _db.Spaces.AsTracking().FirstOrDefaultAsync(s => s.Id == spaceId, ct);

        if (space is null)
            return (false, "Espacio no encontrado");

        // 2) Busca el metadato del archivo
        var meta = await _db.FileResources.FirstOrDefaultAsync(
            fr => fr.SpaceId == spaceId && fr.RelativePath == relativePath,
            ct
        );

        if (meta is null)
            return (false, "Archivo no registrado");

        // 3) Construye la ruta física (evita traversal)
        var safeRelativePath = relativePath.Replace('\\', '/').TrimStart('/').Trim();
        var fullPath = Path.Combine(_rootPath, space.CustomerId.ToString("N"), safeRelativePath);
        var normalizedRoot = Path.GetFullPath(
            Path.Combine(_rootPath, space.CustomerId.ToString("N"))
        );
        var normalizedPath = Path.GetFullPath(fullPath);

        if (!normalizedPath.StartsWith(normalizedRoot)) // intento de salir de la carpeta
            return (false, "Ruta no válida");

        // 4) Borra el archivo físico
        if (File.Exists(normalizedPath))
            File.Delete(normalizedPath);

        // 5) Actualiza UsedBytes y borra metadato
        space.UsedBytes = Math.Max(space.UsedBytes - fileBytes, 0);
        _db.FileResources.Remove(meta);

        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "Archivo {RelPath} eliminado de Space {SpaceId}",
            relativePath,
            spaceId
        );
        return (true, null)!;
    }

#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    public async Task<FileResource?> FindMetaAsync(
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
        Guid customerId,
        string relativePath,
        CancellationToken ct
    )
    {
        var space = await _db
            .Spaces.AsNoTracking()
            .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);
        if (space is null)
        {
            _log.LogWarning("No se encontró el espacio para el cliente {CustomerId}", customerId);
            return null!;
        }

        return await _db
            .FileResources.AsNoTracking()
            .Where(fr => fr.SpaceId == space.Id && fr.RelativePath == relativePath)
            .FirstOrDefaultAsync(ct);
    }

    public async Task EnsureStructureAsync(
        Guid customerId,
        IEnumerable<string> folders,
        CancellationToken ct = default
    )
    {
        var yearFolder = Path.Combine(_rootPath, DateTime.UtcNow.Year.ToString());
        var customerFolder = Path.Combine(yearFolder, customerId.ToString("N"));

        Directory.CreateDirectory(customerFolder);

        foreach (var f in folders.Distinct(StringComparer.OrdinalIgnoreCase))
            Directory.CreateDirectory(Path.Combine(customerFolder, f));

        var existingSpace = await _db
            .Spaces.AsTracking()
            .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);

        if (existingSpace == null)
        {
            var newSpace = new Space
            {
                CustomerId = customerId,
                MaxBytes = 5L * 1024 * 1024 * 1024, // 5 GB
                UsedBytes = 0,
                CreateAt = DateTime.UtcNow,
            };

            await _db.Spaces.AddAsync(newSpace, ct);
            await _db.SaveChangesAsync(ct);

            _log.LogInformation(
                "Space creado en BD para cliente {CustomerId} con cuota de {MaxGB} GB",
                customerId,
                newSpace.MaxBytes / (1024 * 1024 * 1024)
            );
        }
        else
        {
            _log.LogInformation(
                "Space ya existe en BD para cliente {CustomerId} con cuota de {MaxGB} GB",
                customerId,
                existingSpace.MaxBytes / (1024 * 1024 * 1024)
            );
        }

        _log.LogInformation(
            "Estructura de {CustomerId} lista en {Path}",
            customerId,
            customerFolder
        );
    }
}
