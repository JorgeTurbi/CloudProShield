using AutoMapper;
using CloudShield.DTOs.FileSystem;
using CloudShield.Services.FileSystemServices;
using Commons;
using DataContext;
using Microsoft.EntityFrameworkCore;

namespace CloudShield.Services.FileSystemRead_Repository;

public class FileSystemRead_Repository : IFileSystemReadService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FileSystemRead_Repository> _logger;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly string _rootPath;

    public FileSystemRead_Repository(
        ApplicationDbContext context,
        ILogger<FileSystemRead_Repository> logger,
        IMapper mapper,
        IConfiguration configuration
    )
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
        _configuration = configuration;
        _rootPath = _configuration["Storage:RootPath"] ?? "storage";
    }

    public async Task<ApiResponse<CustomerFolderStructureDTO>> GetCustomerFolderStructureAsync(
        Guid customerId,
        CancellationToken ct = default
    )
    {
        try
        {
            var space = await _context
                .Spaces.AsNoTracking()
                .Include(s => s.FileResources)
                .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);

            if (space == null)
            {
                return new ApiResponse<CustomerFolderStructureDTO>(
                    false,
                    "No se encontró espacio para el cliente",
                    null
                );
            }

            var currentYear = DateTime.UtcNow.Year.ToString();
            var customerPath = Path.Combine(_rootPath, currentYear, customerId.ToString("N"));

            var result = new CustomerFolderStructureDTO
            {
                CustomerId = customerId,
                Year = currentYear,
                UsedBytes = space.UsedBytes,
                MaxBytes = space.MaxBytes,
                TotalFiles = space.FileResources.Count,
                TotalSizeBytes = space.FileResources.Sum(f => f.SizeBytes),
            };

            if (Directory.Exists(customerPath))
            {
                var folders = new List<FolderDTO>();
                var subDirectories = Directory.GetDirectories(customerPath);

                foreach (var dirPath in subDirectories)
                {
                    var dirName = Path.GetFileName(dirPath);
                    var filesInFolder = space
                        .FileResources.Where(f => f.RelativePath.StartsWith(dirName + "/"))
                        .ToList();

                    folders.Add(
                        new FolderDTO
                        {
                            Name = dirName,
                            FullPath = dirPath,
                            CreatedAt = Directory.GetCreationTime(dirPath),
                            FileCount = filesInFolder.Count,
                            TotalSizeBytes = filesInFolder.Sum(f => f.SizeBytes),
                        }
                    );
                }

                result.Folders = folders;
            }

            _logger.LogInformation(
                "Estructura de carpetas obtenida para cliente {CustomerId}: {FolderCount} carpetas",
                customerId,
                result.Folders.Count
            );

            return new ApiResponse<CustomerFolderStructureDTO>(
                true,
                $"Estructura obtenida correctamente. {result.Folders.Count} carpetas encontradas",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al obtener estructura de carpetas para cliente {CustomerId}",
                customerId
            );
            return new ApiResponse<CustomerFolderStructureDTO>(
                false,
                "Error interno al obtener estructura de carpetas",
                null
            );
        }
    }

    public async Task<ApiResponse<FolderContentDTO>> GetFolderContentAsync(
        Guid customerId,
        string folderPath,
        CancellationToken ct = default
    )
    {
        folderPath = folderPath.Trim('/'); // “Cargar/Cagrando”
        var currentYear = DateTime.UtcNow.Year.ToString();
        var basePath = Path.Combine(_rootPath, currentYear, customerId.ToString("N"), folderPath);

        /* 2.a ► SUB-CARPETAS físicas */
        var subDirs = Directory.Exists(basePath)
            ? Directory.GetDirectories(basePath)
            : Array.Empty<string>();

        /* 2.b ► METADATOS de archivos SÓLO del nivel actual */
        var space = await _context
            .Spaces.AsNoTracking()
            .Include(s => s.FileResources)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);

        var subFolderDTOs = subDirs
            .Select(dir =>
            {
                var dirName = Path.GetFileName(dir); // “2024”, “Fotos”
                // Archivos que viven EN cualquier nivel dentro de esa sub-carpeta
                var filesBelow = space
                    .FileResources.Where(f =>
                        f.RelativePath.StartsWith(
                            $"{folderPath.TrimEnd('/')}/{dirName}/",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .ToList();

                return new FolderDTO
                {
                    Name = dirName,
                    FullPath = dir,
                    CreatedAt = Directory.GetCreationTime(dir),
                    FileCount = filesBelow.Count,
                    TotalSizeBytes = filesBelow.Sum(f => f.SizeBytes),
                };
            })
            .ToList();

        if (space is null)
            return new(false, "No se encontró espacio para el cliente", null);

        string levelDir = folderPath.Replace('\\', '/');

        var filesInFolder = space
            .FileResources.Where(f =>
                Path.GetDirectoryName(f.RelativePath)!
                    .Replace('\\', '/')
                    .Equals(levelDir, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        var fileItemDTOs = _mapper.Map<List<FileItemDTO>>(filesInFolder);

        var result = new FolderContentDTO
        {
            FolderName = Path.GetFileName(folderPath),
            FolderPath = $"{currentYear}/{customerId:N}/{folderPath}",
            SubFolders = subFolderDTOs, // 👈 se añade
            Files = fileItemDTOs,
            TotalFiles = fileItemDTOs.Count,
            TotalSizeBytes = fileItemDTOs.Sum(f => f.SizeBytes),
        };

        return new(true, $"Contenido de {folderPath} ({result.TotalFiles} archivos)", result);
    }

    public async Task<ApiResponse<List<FolderDTO>>> GetCustomerFoldersAsync(
        Guid customerId,
        CancellationToken ct = default
    )
    {
        try
        {
            var space = await _context
                .Spaces.AsNoTracking()
                .Include(s => s.FileResources)
                .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);

            if (space == null)
            {
                return new ApiResponse<List<FolderDTO>>(
                    false,
                    "No se encontró espacio para el cliente",
                    new List<FolderDTO>()
                );
            }

            var currentYear = DateTime.UtcNow.Year.ToString();
            var customerPath = Path.Combine(_rootPath, currentYear, customerId.ToString("N"));

            var folders = new List<FolderDTO>();

            if (Directory.Exists(customerPath))
            {
                var subDirectories = Directory.GetDirectories(customerPath);

                foreach (var dirPath in subDirectories)
                {
                    var dirName = Path.GetFileName(dirPath);
                    var filesInFolder = space
                        .FileResources.Where(f => f.RelativePath.StartsWith(dirName + "/"))
                        .ToList();

                    folders.Add(
                        new FolderDTO
                        {
                            Name = dirName,
                            FullPath = dirPath,
                            CreatedAt = Directory.GetCreationTime(dirPath),
                            FileCount = filesInFolder.Count,
                            TotalSizeBytes = filesInFolder.Sum(f => f.SizeBytes),
                        }
                    );
                }
            }

            _logger.LogInformation(
                "Carpetas obtenidas para cliente {CustomerId}: {FolderCount} carpetas",
                customerId,
                folders.Count
            );

            return new ApiResponse<List<FolderDTO>>(
                true,
                $"{folders.Count} carpetas encontradas",
                folders
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener carpetas para cliente {CustomerId}", customerId);
            return new ApiResponse<List<FolderDTO>>(
                false,
                "Error interno al obtener carpetas",
                new List<FolderDTO>()
            );
        }
    }

    public async Task<ApiResponse<List<FileItemDTO>>> GetAllCustomerFilesAsync(
        Guid customerId,
        CancellationToken ct = default
    )
    {
        try
        {
            var space = await _context
                .Spaces.AsNoTracking()
                .Include(s => s.FileResources)
                .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);

            if (space == null)
            {
                return new ApiResponse<List<FileItemDTO>>(
                    false,
                    "No se encontró espacio para el cliente",
                    new List<FileItemDTO>()
                );
            }

            var fileItemDTOs = _mapper.Map<List<FileItemDTO>>(space.FileResources.ToList());

            _logger.LogInformation(
                "Archivos obtenidos para cliente {CustomerId}: {FileCount} archivos",
                customerId,
                fileItemDTOs.Count
            );

            return new ApiResponse<List<FileItemDTO>>(
                true,
                $"{fileItemDTOs.Count} archivos encontrados",
                fileItemDTOs
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener archivos para cliente {CustomerId}", customerId);
            return new ApiResponse<List<FileItemDTO>>(
                false,
                "Error interno al obtener archivos",
                new List<FileItemDTO>()
            );
        }
    }
}
