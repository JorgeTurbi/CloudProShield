using AutoMapper;
using CloudShield.Commons.Helpers;
using CloudShield.DTOs.FileSystem;
using CloudShield.Entities.Operations;
using CloudShield.Services.FileSystemServices;
using Commons;
using DataContext;
using Microsoft.EntityFrameworkCore;

namespace CloudShield.Services.FileSystemRead_Repository;

public class FileSystemRead_RepositoryUser : IFileSystemReadServiceUser
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FileSystemRead_RepositoryUser> _logger;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly string _rootPath;

    public FileSystemRead_RepositoryUser(
        ApplicationDbContext context,
        ILogger<FileSystemRead_RepositoryUser> logger,
        IMapper mapper,
        IConfiguration configuration
    )
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
        _configuration = configuration;
        _rootPath = _configuration["Storage:RootPath"] ?? "StorageCloud";
    }

    #region Folder Structure Operations

    /// <summary>
    /// Gets complete folder structure for a user
    /// </summary>
    public async Task<ApiResponse<UserFolderStructureDTO>> GetUserFolderStructureAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        try
        {
            var space = await _context
                .SpacesClouds.AsNoTracking()
                .Include(s => s.FileResourcesCloud)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (space == null)
            {
                return new ApiResponse<UserFolderStructureDTO>(false, "User space not found", null);
            }

            var userRoot = FileStoragePathResolver.UserRoot(_rootPath, userId);
            var folders = await GetFolderStructureRecursive(
                userRoot,
                space.FileResourcesCloud.ToList(),
                string.Empty
            );

            var dto = new UserFolderStructureDTO
            {
                UserId = userId,
                Year = "N/A", // No longer using year-based structure
                UsedBytes = space.UsedBytes,
                MaxBytes = space.MaxBytes,
                TotalFiles = space.FileResourcesCloud.Count,
                TotalSizeBytes = space.FileResourcesCloud.Sum(f => f.SizeBytes),
                Folders = folders,
            };

            _logger.LogInformation(
                "Folder structure retrieved for user {UserId}: {FolderCount} folders",
                userId,
                dto.Folders.Count
            );

            return new ApiResponse<UserFolderStructureDTO>(
                true,
                $"Structure retrieved successfully. {dto.Folders.Count} folders found",
                dto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving folder structure for user {UserId}", userId);
            return new ApiResponse<UserFolderStructureDTO>(
                false,
                "Internal error while retrieving folder structure",
                null
            );
        }
    }

    /// <summary>
    /// Gets content of a specific folder with proper nested navigation
    /// </summary>
    public async Task<ApiResponse<FolderContentDTO>> GetFolderContentAsync(
        Guid userId,
        string folderPath,
        CancellationToken ct = default
    )
    {
        try
        {
            var normalizedPath = FileStoragePathResolver.NormalizePath(folderPath);

            var space = await _context
                .SpacesClouds.AsNoTracking()
                .Include(s => s.FileResourcesCloud)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (space == null)
            {
                return new ApiResponse<FolderContentDTO>(false, "User space not found", null);
            }

            var userRoot = FileStoragePathResolver.UserRoot(_rootPath, userId);
            var fullFolderPath = string.IsNullOrEmpty(normalizedPath)
                ? userRoot
                : FileStoragePathResolver.GetFullPath(_rootPath, userId, normalizedPath);

            // Get direct subfolders only
            var subDirectories = Directory.Exists(fullFolderPath)
                ? Directory.GetDirectories(fullFolderPath)
                : Array.Empty<string>();

            var subFolders = subDirectories
                .Select(dir =>
                {
                    var dirName = Path.GetFileName(dir);
                    var relativeDirPath = string.IsNullOrEmpty(normalizedPath)
                        ? dirName
                        : FileStoragePathResolver.BuildRelativePath(normalizedPath, dirName);

                    var filesInDir = space
                        .FileResourcesCloud.Where(f =>
                            FileStoragePathResolver.IsFileInFolder(f.RelativePath, relativeDirPath)
                        )
                        .ToList();

                    return new FolderDTO
                    {
                        Name = dirName,
                        FullPath = relativeDirPath,
                        CreatedAt = Directory.GetCreationTime(dir),
                        FileCount = filesInDir.Count,
                        TotalSizeBytes = filesInDir.Sum(f => f.SizeBytes),
                    };
                })
                .OrderBy(f => f.Name)
                .ToList();

            // Get files directly in this folder (not in subfolders)
            var filesInCurrentFolder = space
                .FileResourcesCloud.Where(f =>
                    FileStoragePathResolver.IsFileDirectlyInFolder(
                        f.RelativePath, // p.ej.  "Pruebass/balance-sheet.xlsx"
                        normalizedPath
                    )
                ) // p.ej.  "Pruebass"  (o "" para root)
                .OrderBy(f => f.FileName)
                .ToList();

            var dto = new FolderContentDTO
            {
                FolderName = string.IsNullOrEmpty(normalizedPath)
                    ? "Root"
                    : Path.GetFileName(normalizedPath),
                FolderPath = normalizedPath,
                SubFolders = subFolders,
                Files = _mapper.Map<List<FileItemDTO>>(filesInCurrentFolder),
                TotalFiles = filesInCurrentFolder.Count,
                TotalSizeBytes = filesInCurrentFolder.Sum(f => f.SizeBytes),
            };

            _logger.LogInformation(
                "Folder content retrieved for user {UserId}, path '{FolderPath}': {FileCount} files, {FolderCount} subfolders",
                userId,
                normalizedPath,
                dto.Files.Count,
                dto.SubFolders.Count
            );

            return new ApiResponse<FolderContentDTO>(
                true,
                "Folder content retrieved successfully",
                dto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving folder content for user {UserId}, path '{FolderPath}'",
                userId,
                folderPath
            );
            return new ApiResponse<FolderContentDTO>(
                false,
                "Internal error while retrieving folder content",
                null
            );
        }
    }

    /// <summary>
    /// Gets all user folders (flat list)
    /// </summary>
    public async Task<ApiResponse<List<FolderDTO>>> GetUserFoldersAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        try
        {
            var space = await _context
                .SpacesClouds.AsNoTracking()
                .Include(s => s.FileResourcesCloud)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (space == null)
            {
                return new ApiResponse<List<FolderDTO>>(
                    false,
                    "User space not found",
                    new List<FolderDTO>()
                );
            }

            var userPath = FileStoragePathResolver.UserRoot(_rootPath, userId);
            var folders = new List<FolderDTO>();

            if (Directory.Exists(userPath))
            {
                // Get all directories recursively
                var allDirectories = Directory.GetDirectories(
                    userPath,
                    "*",
                    SearchOption.AllDirectories
                );

                foreach (var dirPath in allDirectories)
                {
                    var relativePath = Path.GetRelativePath(userPath, dirPath).Replace('\\', '/');
                    var dirName = Path.GetFileName(dirPath);

                    var filesInFolder = space
                        .FileResourcesCloud.Where(f =>
                            FileStoragePathResolver.IsFileInFolder(f.RelativePath, relativePath)
                        )
                        .ToList();

                    folders.Add(
                        new FolderDTO
                        {
                            Name = dirName,
                            FullPath = relativePath,
                            CreatedAt = Directory.GetCreationTime(dirPath),
                            FileCount = filesInFolder.Count,
                            TotalSizeBytes = filesInFolder.Sum(f => f.SizeBytes),
                        }
                    );
                }
            }

            folders = folders.OrderBy(f => f.FullPath).ToList();

            _logger.LogInformation(
                "Folders retrieved for user {UserId}: {FolderCount} folders",
                userId,
                folders.Count
            );

            return new ApiResponse<List<FolderDTO>>(
                true,
                $"{folders.Count} folders found",
                folders
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving folders for user {UserId}", userId);
            return new ApiResponse<List<FolderDTO>>(
                false,
                "Internal error while retrieving folders",
                new List<FolderDTO>()
            );
        }
    }

    /// <summary>
    /// Gets folder content for exploration (root level navigation)
    /// </summary>
    public async Task<ApiResponse<FolderContentDTO>> GetFolderContentExploreAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        try
        {
            // This method returns root level content for exploration
            return await GetFolderContentAsync(userId, string.Empty, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving explore content for user {UserId}", userId);
            return new ApiResponse<FolderContentDTO>(
                false,
                "Internal error while retrieving explore content",
                null
            );
        }
    }

    #endregion

    #region File Operations

    /// <summary>
    /// Gets all files for a user
    /// </summary>
    public async Task<ApiResponse<List<FileItemDTO>>> GetAllUserFilesAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        try
        {
            var space = await _context
                .SpacesClouds.AsNoTracking()
                .Include(s => s.FileResourcesCloud)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (space == null)
            {
                return new ApiResponse<List<FileItemDTO>>(
                    false,
                    "User space not found",
                    new List<FileItemDTO>()
                );
            }

            var files = space
                .FileResourcesCloud.OrderBy(f => f.RelativePath)
                .ThenBy(f => f.FileName)
                .ToList();

            var fileItems = _mapper.Map<List<FileItemDTO>>(files);

            _logger.LogInformation(
                "All files retrieved for user {UserId}: {FileCount} files",
                userId,
                fileItems.Count
            );

            return new ApiResponse<List<FileItemDTO>>(
                true,
                $"{fileItems.Count} files found",
                fileItems
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all files for user {UserId}", userId);
            return new ApiResponse<List<FileItemDTO>>(
                false,
                "Internal error while retrieving files",
                new List<FileItemDTO>()
            );
        }
    }

    /// <summary>
    /// Gets space information for a user
    /// </summary>
    public async Task<ApiResponse<SpaceCloudDTO>> GetAllSpaceAsync(
        string userIdString,
        CancellationToken ct = default
    )
    {
        try
        {
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return new ApiResponse<SpaceCloudDTO>(false, "Invalid user ID format", null);
            }

            var spaceDto = await _context
                .SpacesClouds.AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new SpaceCloudDTO
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    MaxBytes = s.MaxBytes,
                    UsedBytes = s.UsedBytes,
                    RowVersion = s.RowVersion,
                    CreatedAt = s.CreateAt,
                    UpdatedAt = s.UpdateAt,
                    Files = s
                        .FileResourcesCloud.Select(f => new FileResourceCloudDTO
                        {
                            Id = f.Id,
                            FileName = f.FileName,
                            FileSize = f.SizeBytes,
                            ContentType = f.ContentType,
                            CreatedAt = f.CreateAt,
                        })
                        .ToList(),
                })
                .FirstOrDefaultAsync(ct);

            if (spaceDto == null)
            {
                return new ApiResponse<SpaceCloudDTO>(false, "User space not found", null);
            }

            _logger.LogInformation(
                "Space information retrieved for user {UserId}: {UsedBytes}/{MaxBytes} bytes",
                userId,
                spaceDto.UsedBytes,
                spaceDto.MaxBytes
            );

            return new ApiResponse<SpaceCloudDTO>(
                true,
                "Space information retrieved successfully",
                spaceDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving space for user {UserId}", userIdString);
            return new ApiResponse<SpaceCloudDTO>(
                false,
                "Internal error while retrieving space information",
                null
            );
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Recursively builds folder structure
    /// </summary>
    private async Task<List<FolderDTO>> GetFolderStructureRecursive(
        string basePath,
        List<FileResourceCloud> allFiles,
        string currentRelativePath
    )
    {
        var folders = new List<FolderDTO>();
        var currentFullPath = string.IsNullOrEmpty(currentRelativePath)
            ? basePath
            : Path.Combine(basePath, currentRelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(currentFullPath))
            return folders;

        try
        {
            var subDirectories = Directory.GetDirectories(currentFullPath);

            foreach (var subDir in subDirectories)
            {
                var dirName = Path.GetFileName(subDir);
                var relativeDirPath = string.IsNullOrEmpty(currentRelativePath)
                    ? dirName
                    : FileStoragePathResolver.BuildRelativePath(currentRelativePath, dirName);

                // Get files in this specific directory and all subdirectories
                var filesInDir = allFiles
                    .Where(f =>
                        FileStoragePathResolver.IsFileInFolder(f.RelativePath, relativeDirPath)
                    )
                    .ToList();

                var folder = new FolderDTO
                {
                    Name = dirName,
                    FullPath = relativeDirPath,
                    CreatedAt = Directory.GetCreationTime(subDir),
                    FileCount = filesInDir.Count,
                    TotalSizeBytes = filesInDir.Sum(f => f.SizeBytes),
                };

                folders.Add(folder);

                // Recursively get subfolders
                var subFolders = await GetFolderStructureRecursive(
                    basePath,
                    allFiles,
                    relativeDirPath
                );
                folders.AddRange(subFolders);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error reading directory structure at path: {Path}",
                currentFullPath
            );
        }

        return folders.OrderBy(f => f.FullPath).ToList();
    }

    #endregion
}
