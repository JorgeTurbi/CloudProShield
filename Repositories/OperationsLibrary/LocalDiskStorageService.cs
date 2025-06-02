
using System.Linq;
using CloudShield.Entities.Operations;
using DataContext;
using Microsoft.EntityFrameworkCore;

namespace CloudShield.Services.OperationStorage;

public class LocalDiskStorageService : IStorageService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<LocalDiskStorageService> _log;
    private readonly string _rootPath;

    public LocalDiskStorageService(IConfiguration cfg,
                                   ApplicationDbContext db,
                                   ILogger<LocalDiskStorageService> log)
    {
        _rootPath = cfg["Storage:RootPath"] ?? "storage"; // configurable
        _db = db;
        _log = log;
    }

    /* ----------------------------------------------------------- */
    /*  MÉTODO 1: Guardar y segmentar                              */
    /* ----------------------------------------------------------- */
    public async Task<(bool ok, string relativePathOrReason)> SaveFileAsync(
            Guid customerId,
            IFormFile file,
            CancellationToken ct)
    {
        // 1) Obtiene (o crea) el espacio
        var space = await _db.Spaces.AsTracking()
                          .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);

        if (space is null)
        {
            space = new Space
            {
                CustomerId = customerId,
                MaxBytes = 5L * 1024 * 1024 * 1024, // 5 GB
                UsedBytes = 0
            };
            await _db.Spaces.AddAsync(space, ct);
            await _db.SaveChangesAsync(ct);
        }

        // 2) Cuota
        if (space.UsedBytes + file.Length > space.MaxBytes)
            return (false, "Se supera la cuota asignada");

        // 3) Segmentación por tipo
        var category = GetCategory(Path.GetExtension(file.FileName), file.ContentType);
        var customerFolder = Path.Combine(_rootPath, customerId.ToString("N"));
        var categoryFolder = Path.Combine(customerFolder, category);

        Directory.CreateDirectory(categoryFolder);

        var safeFileName = Path.GetFileName(file.FileName);
        var filePath = Path.Combine(categoryFolder, safeFileName);
        var relativePath = Path.Combine(category, safeFileName)         // p. ej. "images/logo.png"
                              .Replace('\\', '/');                      // normaliza

        // 4) Guarda en disco
        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(stream, ct);
        }

        // 5) Guarda metadatos
        var meta = new FileResource
        {

            SpaceId = space.Id,
            FileName = safeFileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            RelativePath = relativePath,
            SizeBytes = file.Length,
            CreateAt = DateTime.UtcNow
        };
        await _db.FileResources.AddAsync(meta, ct);

        space.UsedBytes += file.Length;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Archivo {File} ({Cat}) guardado para {Customer}",
                            safeFileName, category, customerId);

        return (true, relativePath);
    }

    /* ----------------------------------------------------------- */
    /*  MÉTODO 2: Obtener archivo por path                         */
    /* ----------------------------------------------------------- */
    public async Task<(bool ok, Stream content, string contentType, string reason)> GetFileAsync(
            Guid customerId,
            string relativePath,
            CancellationToken ct)
    {
        var space = await _db.Spaces
                             .AsNoTracking()
                             .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);

        if (space is null)
            return (false, null, null, "Espacio no encontrado")!;

        // Verifica que el archivo pertenezca al cliente
        var meta = await _db.FileResources
                            .AsNoTracking().Where(fr=>fr.SpaceId == space.Id && fr.RelativePath == relativePath)
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
            int spaceId,
            string relativePath,
            long fileBytes,
            CancellationToken ct)
    {
        // 1) Obtiene el espacio
        var space = await _db.Spaces
                             .AsTracking()
                             .FirstOrDefaultAsync(s => s.Id == spaceId, ct);

        if (space is null)
            return (false, "Espacio no encontrado");

        // 2) Busca el metadato del archivo
        var meta = await _db.FileResources
                            .FirstOrDefaultAsync(fr => fr.SpaceId == spaceId &&
                                                       fr.RelativePath == relativePath, ct);

        if (meta is null)
            return (false, "Archivo no registrado");

        // 3) Construye la ruta física (evita traversal)
        var safeRelativePath = relativePath.Replace('\\', '/')
                                           .TrimStart('/')
                                           .Trim();
        var fullPath = Path.Combine(_rootPath, space.CustomerId.ToString("N"), safeRelativePath);
        var normalizedRoot = Path.GetFullPath(Path.Combine(_rootPath, space.CustomerId.ToString("N")));
        var normalizedPath = Path.GetFullPath(fullPath);

        if (!normalizedPath.StartsWith(normalizedRoot))   // intento de salir de la carpeta
            return (false, "Ruta no válida");

        // 4) Borra el archivo físico
        if (File.Exists(normalizedPath))
            File.Delete(normalizedPath);

        // 5) Actualiza UsedBytes y borra metadato
        space.UsedBytes = Math.Max(space.UsedBytes - fileBytes, 0);
        _db.FileResources.Remove(meta);

        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Archivo {RelPath} eliminado de Space {SpaceId}", relativePath, spaceId);
        return (true, null)!;
    }

#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    public async Task<FileResource?> FindMetaAsync(
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
          Guid customerId,
          string relativePath,
          CancellationToken ct)
    {
        var space = await _db.Spaces
                             .AsNoTracking()
                             .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);
        if (space is null)
        {
                                
            _log.LogWarning("No se encontró el espacio para el cliente {CustomerId}", customerId);
            return null!;
                             }
       

            return await _db.FileResources
                       .AsNoTracking().Where(fr => fr.SpaceId == space.Id &&
                                                fr.RelativePath == relativePath)
                       .FirstOrDefaultAsync(ct);
    }

}
