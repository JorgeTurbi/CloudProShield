using System.IO.Compression;
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
        _rootPath = cfg["Storage:RootPath"] ?? "StorageCloud";
        _db = db;
        _log = log;
    }

    #region Private Helper Methods

    /// <summary>
    /// Ensures user structure and space exist
    /// </summary>
    private async Task EnsureUserStructureAsync(
        Guid userId,
        IEnumerable<string> extraFolders,
        CancellationToken ct = default
    )
    {
        var userRoot = FileStoragePathResolver.UserRoot(_rootPath, userId);

        // Create root directory
        Directory.CreateDirectory(userRoot);

        // Create additional folders with proper path handling
        foreach (var folderPath in extraFolders.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var normalizedPath = FileStoragePathResolver.NormalizePath(folderPath);
            if (!string.IsNullOrEmpty(normalizedPath))
            {
                var fullPath = FileStoragePathResolver.GetFullPath(
                    _rootPath,
                    userId,
                    normalizedPath
                );
                Directory.CreateDirectory(fullPath);
            }
        }

        // Ensure SpaceCloud record exists
        var space = await _db.SpacesClouds.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (space == null)
        {
            space = new SpaceCloud
            {
                UserId = userId,
                MaxBytes = 5L * 1024 * 1024 * 1024, // 5 GB default
                UsedBytes = 0,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                RowVersion = Array.Empty<byte>(),
            };
            await _db.SpacesClouds.AddAsync(space, ct);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Sanitizes and validates folder names
    /// </summary>
    private static string SanitizeFolderPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        // Remove dangerous characters but keep path separators
        var sanitized = Regex.Replace(path, @"[^\w\-\. /\\]", "");
        return FileStoragePathResolver.NormalizePath(sanitized);
    }

    #endregion

    #region File Operations

    /// <summary>
    /// Saves a file to the user's storage space
    /// </summary>
    public async Task<(bool ok, string relativePathOrReason)> SaveFileAsync(
        Guid userId,
        IFormFile file,
        CancellationToken ct,
        string? customFolder = null
    )
    {
        try
        {
            // Validate input
            if (file == null || file.Length == 0)
                return (false, "File is empty or null");

            // Get or create space
            var space = await _db.SpacesClouds.FirstOrDefaultAsync(s => s.UserId == userId, ct);
            if (space == null)
            {
                await EnsureUserStructureAsync(userId, Array.Empty<string>(), ct);
                space = await _db.SpacesClouds.FirstAsync(s => s.UserId == userId, ct);
            }

            // Check quota
            if (space.UsedBytes + file.Length > space.MaxBytes)
                return (false, "Storage quota exceeded");

            // Process folder path
            var sanitizedFolder = SanitizeFolderPath(customFolder ?? string.Empty);
            var fileName = Path.GetFileName(file.FileName);

            // Build paths
            var relativePath = string.IsNullOrEmpty(sanitizedFolder)
                ? fileName
                : $"{sanitizedFolder}/{fileName}";

            var fullPath = FileStoragePathResolver.GetFullPath(_rootPath, userId, relativePath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save file
            await using (
                var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write)
            )
            {
                await file.CopyToAsync(fileStream, ct);
            }

            // Save metadata
            var metadata = new FileResourceCloud
            {
                SpaceId = space.Id,
                FileName = fileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                RelativePath = relativePath,
                SizeBytes = file.Length,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
            };

            await _db.FileResourcesCloud.AddAsync(metadata, ct);

            // Update space usage
            space.UsedBytes += file.Length;
            space.UpdateAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _log.LogInformation(
                "File {FileName} saved for user {UserId} at {RelativePath}",
                fileName,
                userId,
                relativePath
            );

            return (true, relativePath);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error saving file for user {UserId}", userId);
            return (false, "Internal error while saving file");
        }
    }

    /// <summary>
    /// Retrieves a file from user's storage
    /// </summary>
    public async Task<(bool ok, Stream content, string contentType, string reason)> GetFileAsync(
        Guid userId,
        string relativePath,
        CancellationToken ct
    )
    {
        try
        {
            var normalizedPath = FileStoragePathResolver.NormalizePath(relativePath);

            var space = await _db
                .SpacesClouds.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (space == null)
                return (false, null, null, "User space not found")!;

            var metadata = await _db
                .FileResourcesCloud.AsNoTracking()
                .FirstOrDefaultAsync(
                    fr => fr.SpaceId == space.Id && fr.RelativePath == normalizedPath,
                    ct
                );

            if (metadata == null)
                return (false, null, null, "File not found in database")!;

            var fullPath = FileStoragePathResolver.GetFullPath(_rootPath, userId, normalizedPath);

            if (!File.Exists(fullPath))
                return (false, null, null, "Physical file not found")!;

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (true, stream, metadata.ContentType, null)!;
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error retrieving file {RelativePath} for user {UserId}",
                relativePath,
                userId
            );
            return (false, null, null, "Internal error while retrieving file")!;
        }
    }

    /// <summary>
    /// Deletes a file from user's storage
    /// </summary>
    public async Task<(bool ok, string reason)> DeleteFileAsync(
        Guid spaceId,
        string relativePath,
        long fileBytes,
        CancellationToken ct
    )
    {
        try
        {
            var normalizedPath = FileStoragePathResolver.NormalizePath(relativePath);

            var space = await _db.SpacesClouds.FirstOrDefaultAsync(s => s.Id == spaceId, ct);
            if (space == null)
                return (false, "Space not found");

            var metadata = await _db.FileResourcesCloud.FirstOrDefaultAsync(
                fr => fr.SpaceId == spaceId && fr.RelativePath == normalizedPath,
                ct
            );

            if (metadata == null)
                return (false, "File metadata not found");

            // Delete physical file
            var fullPath = FileStoragePathResolver.GetFullPath(
                _rootPath,
                space.UserId,
                normalizedPath
            );
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Update database
            space.UsedBytes = Math.Max(space.UsedBytes - fileBytes, 0);
            space.UpdateAt = DateTime.UtcNow;
            _db.FileResourcesCloud.Remove(metadata);

            await _db.SaveChangesAsync(ct);

            _log.LogInformation(
                "File {RelativePath} deleted for user {UserId}",
                normalizedPath,
                space.UserId
            );
            return (true, null);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error deleting file {RelativePath}", relativePath);
            return (false, "Internal error while deleting file");
        }
    }

    #endregion

    #region Folder Operations

    /// <summary>
    /// Creates a folder in user's storage
    /// </summary>
    public async Task<ApiResponse<object>> CreateFolderAsync(
        Guid userId,
        string relativePath,
        CancellationToken ct
    )
    {
        try
        {
            var sanitizedPath = SanitizeFolderPath(relativePath);
            if (string.IsNullOrWhiteSpace(sanitizedPath))
                return new ApiResponse<object>(false, "Invalid folder path", null);

            await EnsureUserStructureAsync(userId, new[] { sanitizedPath }, ct);

            _log.LogInformation(
                "Folder {FolderPath} created for user {UserId}",
                sanitizedPath,
                userId
            );
            return new ApiResponse<object>(
                true,
                "Folder created successfully",
                new { path = sanitizedPath }
            );
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error creating folder {FolderPath} for user {UserId}",
                relativePath,
                userId
            );
            return new ApiResponse<object>(false, "Internal error while creating folder", null);
        }
    }

    /// <summary>
    /// Deletes a folder and all its contents
    /// </summary>
    public async Task<(bool ok, string reason)> DeleteFolderAsync(
        Guid userId,
        string folderPath,
        CancellationToken ct
    )
    {
        try
        {
            var sanitizedPath = SanitizeFolderPath(folderPath);
            if (string.IsNullOrWhiteSpace(sanitizedPath))
                return (false, "Invalid folder path");

            var space = await _db
                .SpacesClouds.Include(s => s.FileResourcesCloud)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (space == null)
                return (false, "User space not found");

            // Find all files in the folder and subfolders
            var filesToDelete = space
                .FileResourcesCloud.Where(f =>
                    f.RelativePath.StartsWith(
                        sanitizedPath + "/",
                        StringComparison.OrdinalIgnoreCase
                    ) || f.RelativePath.Equals(sanitizedPath, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            // Delete files from database
            _db.FileResourcesCloud.RemoveRange(filesToDelete);

            // Update space usage
            var totalDeletedBytes = filesToDelete.Sum(f => f.SizeBytes);
            space.UsedBytes = Math.Max(space.UsedBytes - totalDeletedBytes, 0);
            space.UpdateAt = DateTime.UtcNow;

            // Delete physical directory
            var fullPath = FileStoragePathResolver.GetFullPath(_rootPath, userId, sanitizedPath);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }

            await _db.SaveChangesAsync(ct);

            _log.LogInformation(
                "Folder {FolderPath} deleted for user {UserId}",
                sanitizedPath,
                userId
            );
            return (true, null);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error deleting folder {FolderPath} for user {UserId}",
                folderPath,
                userId
            );
            return (false, "Internal error while deleting folder");
        }
    }

    /// <summary>
    /// Creates a ZIP file of a folder's contents
    /// </summary>
    public async Task<(bool ok, Stream content, string reason)> GetFolderZipAsync(
        Guid userId,
        string folderPath,
        CancellationToken ct
    )
    {
        try
        {
            var sanitizedPath = SanitizeFolderPath(folderPath);
            if (string.IsNullOrWhiteSpace(sanitizedPath))
                return (false, null, "Invalid folder path");

            var space = await _db
                .SpacesClouds.Include(s => s.FileResourcesCloud)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (space == null)
                return (false, null, "User space not found");

            var filesInFolder = space
                .FileResourcesCloud.Where(f =>
                    f.RelativePath.StartsWith(
                        sanitizedPath + "/",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToList();

            if (!filesInFolder.Any())
                return (false, null, "Folder is empty or does not exist");

            var memoryStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var fileMetadata in filesInFolder)
                {
                    var fullPath = FileStoragePathResolver.GetFullPath(
                        _rootPath,
                        userId,
                        fileMetadata.RelativePath
                    );
                    if (File.Exists(fullPath))
                    {
                        var entryName = fileMetadata.RelativePath.Replace('\\', '/');
                        zipArchive.CreateEntryFromFile(
                            fullPath,
                            entryName,
                            CompressionLevel.Fastest
                        );
                    }
                }
            }

            memoryStream.Position = 0;
            return (true, memoryStream, null);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error creating ZIP for folder {FolderPath} for user {UserId}",
                folderPath,
                userId
            );
            return (false, null, "Internal error while creating ZIP file");
        }
    }

    /// <summary>
    /// Creates a ZIP file and returns as byte array
    /// </summary>
    public async Task<byte[]> CreateFolderZipAsync(
        Guid userId,
        string folderPath,
        CancellationToken ct = default
    )
    {
        try
        {
            var sanitizedPath = SanitizeFolderPath(folderPath);
            var fullPath = FileStoragePathResolver.GetFullPath(_rootPath, userId, sanitizedPath);

            if (!Directory.Exists(fullPath))
                return null!;

            await using var memoryStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(fullPath, file);
                    zipArchive.CreateEntryFromFile(
                        file,
                        relativePath.Replace('\\', '/'),
                        CompressionLevel.Fastest
                    );
                }
            }

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error creating folder ZIP for user {UserId}, folder {FolderPath}",
                userId,
                folderPath
            );
            return null!;
        }
    }

    #endregion

    #region Metadata Operations

    /// <summary>
    /// Finds file metadata by relative path
    /// </summary>
    public async Task<FileResourceCloud?> FindMetaAsyncUser(
        Guid userId,
        string relativePath,
        CancellationToken ct
    )
    {
        try
        {
            var normalizedPath = FileStoragePathResolver.NormalizePath(relativePath);

            var space = await _db
                .SpacesClouds.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (space == null)
                return null;

            return await _db
                .FileResourcesCloud.AsNoTracking()
                .FirstOrDefaultAsync(
                    fr => fr.SpaceId == space.Id && fr.RelativePath == normalizedPath,
                    ct
                );
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error finding metadata for {RelativePath} for user {UserId}",
                relativePath,
                userId
            );
            return null;
        }
    }

    #endregion
}
