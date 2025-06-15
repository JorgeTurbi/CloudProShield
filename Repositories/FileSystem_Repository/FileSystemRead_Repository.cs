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
        string folderName,
        CancellationToken ct = default
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return new ApiResponse<FolderContentDTO>(
                    false,
                    "El nombre de la carpeta es requerido",
                    null
                );
            }

            var space = await _context
                .Spaces.AsNoTracking()
                .Include(s => s.FileResources)
                .FirstOrDefaultAsync(s => s.CustomerId == customerId, ct);

            if (space == null)
            {
                return new ApiResponse<FolderContentDTO>(
                    false,
                    "No se encontró espacio para el cliente",
                    null
                );
            }

            var filesInFolder = space
                .FileResources.Where(f =>
                    f.RelativePath.StartsWith(folderName + "/", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            var fileItemDTOs = _mapper.Map<List<FileItemDTO>>(filesInFolder);

            var result = new FolderContentDTO
            {
                FolderName = folderName,
                FolderPath = $"{DateTime.UtcNow.Year}/{customerId:N}/{folderName}",
                Files = fileItemDTOs,
                TotalFiles = fileItemDTOs.Count,
                TotalSizeBytes = fileItemDTOs.Sum(f => f.SizeBytes),
            };

            _logger.LogInformation(
                "Contenido de carpeta {FolderName} obtenido para cliente {CustomerId}: {FileCount} archivos",
                folderName,
                customerId,
                result.TotalFiles
            );

            return new ApiResponse<FolderContentDTO>(
                true,
                $"Contenido de {folderName} obtenido correctamente. {result.TotalFiles} archivos encontrados",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al obtener contenido de carpeta {FolderName} para cliente {CustomerId}",
                folderName,
                customerId
            );
            return new ApiResponse<FolderContentDTO>(
                false,
                "Error interno al obtener contenido de carpeta",
                null
            );
        }
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
