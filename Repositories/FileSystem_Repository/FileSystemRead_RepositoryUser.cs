using AutoMapper;
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
      IConfiguration configuration)
  {
    _context = context;
    _logger = logger;
    _mapper = mapper;
    _configuration = configuration;
    _rootPath = _configuration["Storage:RootPath"] ?? "storage";
  }

  public async Task<ApiResponse<UserFolderStructureDTO>> GetCustomerFolderStructureAsync(
      Guid UserId,
      CancellationToken ct = default)
  {
    try
    {
      var space = await _context.SpacesClouds
          .AsNoTracking()
          .Include(s => s.FileResourcesCloud)
          .FirstOrDefaultAsync(s => s.UserId == UserId, ct);

      if (space == null)
      {
        return new ApiResponse<UserFolderStructureDTO>(
            false,
            "No se encontró espacio para el cliente",
            null);
      }

      var currentYear = DateTime.UtcNow.Year.ToString();
      var customerPath = Path.Combine(_rootPath, currentYear, UserId.ToString("N"));

      var result = new UserFolderStructureDTO
      {
        UserId = UserId,
        Year = currentYear,
        UsedBytes = space.UsedBytes,
        MaxBytes = space.MaxBytes,
        TotalFiles = space.FileResourcesCloud.Count,
        TotalSizeBytes = space.FileResourcesCloud.Sum(f => f.SizeBytes)
      };

      if (Directory.Exists(customerPath))
      {
        var folders = new List<FolderDTO>();
        var subDirectories = Directory.GetDirectories(customerPath);

        foreach (var dirPath in subDirectories)
        {
          var dirName = Path.GetFileName(dirPath);
          var filesInFolder = space.FileResourcesCloud
              .Where(f => f.RelativePath.StartsWith(dirName + "/"))
              .ToList();

          folders.Add(new FolderDTO
          {
            Name = dirName,
            FullPath = dirPath,
            CreatedAt = Directory.GetCreationTime(dirPath),
            FileCount = filesInFolder.Count,
            TotalSizeBytes = filesInFolder.Sum(f => f.SizeBytes)
          });
        }

        result.Folders = folders;
      }

      _logger.LogInformation(
          "Estructura de carpetas obtenida para cliente {UserId}: {FolderCount} carpetas",
          UserId,
          result.Folders.Count);

      return new ApiResponse<UserFolderStructureDTO>(
          true,
          $"Estructura obtenida correctamente. {result.Folders.Count} carpetas encontradas",
          result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error al obtener estructura de carpetas para cliente {UserId}", UserId);
      return new ApiResponse<UserFolderStructureDTO>(
          false,
          "Error interno al obtener estructura de carpetas",
          null);
    }
  }

  public async Task<ApiResponse<FolderContentDTO>> GetFolderContentAsync(
      Guid UserId,
      string folderName,
      CancellationToken ct = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(folderName))
      {
        return new ApiResponse<FolderContentDTO>(
            false,
            "El nombre de la carpeta es requerido",
            null);
      }

      var space = await _context.SpacesClouds
          .AsNoTracking()
          .Include(s => s.FileResourcesCloud)
          .FirstOrDefaultAsync(s => s.UserId == UserId, ct);

      if (space == null)
      {
        return new ApiResponse<FolderContentDTO>(
            false,
            "No se encontró espacio para el cliente",
            null);
      }

      var filesInFolder = space.FileResourcesCloud
          .Where(f => f.RelativePath.StartsWith(folderName + "/", StringComparison.OrdinalIgnoreCase))
          .ToList();

      var fileItemDTOs = _mapper.Map<List<FileItemDTO>>(filesInFolder);

      var result = new FolderContentDTO
      {
        FolderName = folderName,
        FolderPath = $"{DateTime.UtcNow.Year}/{UserId:N}/{folderName}",
        Files = fileItemDTOs,
        TotalFiles = fileItemDTOs.Count,
        TotalSizeBytes = fileItemDTOs.Sum(f => f.SizeBytes)
      };

      _logger.LogInformation(
          "Contenido de carpeta {FolderName} obtenido para cliente {UserId}: {FileCount} archivos",
          folderName,
          UserId,
          result.TotalFiles);

      return new ApiResponse<FolderContentDTO>(
          true,
          $"Contenido de {folderName} obtenido correctamente. {result.TotalFiles} archivos encontrados",
          result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error al obtener contenido de carpeta {FolderName} para cliente {UserId}", folderName, UserId);
      return new ApiResponse<FolderContentDTO>(
          false,
          "Error interno al obtener contenido de carpeta",
          null);
    }
  }

  public async Task<ApiResponse<List<FolderDTO>>> GetCustomerFoldersAsync(
      Guid UserId,
      CancellationToken ct = default)
  {
    try
    {
      var space = await _context.SpacesClouds
          .AsNoTracking()
          .Include(s => s.FileResourcesCloud)
          .FirstOrDefaultAsync(s => s.UserId == UserId, ct);

      if (space == null)
      {
        return new ApiResponse<List<FolderDTO>>(
            false,
            "No se encontró espacio para el cliente",
            new List<FolderDTO>());
      }

      var currentYear = DateTime.UtcNow.Year.ToString();
      var customerPath = Path.Combine(_rootPath, currentYear, UserId.ToString("N"));

      var folders = new List<FolderDTO>();

      if (Directory.Exists(customerPath))
      {
        var subDirectories = Directory.GetDirectories(customerPath);

        foreach (var dirPath in subDirectories)
        {
          var dirName = Path.GetFileName(dirPath);
          var filesInFolder = space.FileResourcesCloud
              .Where(f => f.RelativePath.StartsWith(dirName + "/"))
              .ToList();

          folders.Add(new FolderDTO
          {
            Name = dirName,
            FullPath = dirPath,
            CreatedAt = Directory.GetCreationTime(dirPath),
            FileCount = filesInFolder.Count,
            TotalSizeBytes = filesInFolder.Sum(f => f.SizeBytes)
          });
        }
      }

      _logger.LogInformation(
          "Carpetas obtenidas para cliente {UserId}: {FolderCount} carpetas",
          UserId,
          folders.Count);

      return new ApiResponse<List<FolderDTO>>(
          true,
          $"{folders.Count} carpetas encontradas",
          folders);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error al obtener carpetas para cliente {UserId}", UserId);
      return new ApiResponse<List<FolderDTO>>(
          false,
          "Error interno al obtener carpetas",
          new List<FolderDTO>());
    }
  }

  public async Task<ApiResponse<List<FileItemDTO>>> GetAllCustomerFilesAsync(
      Guid UserId,
      CancellationToken ct = default)
  {
    try
    {
      var space = await _context.SpacesClouds
          .AsNoTracking()
          .Include(s => s.FileResourcesCloud)
          .FirstOrDefaultAsync(s => s.UserId == UserId, ct);

         _logger.LogInformation(
          "Archivos Con Informacion de usuario {UserId} solicitados",
         space.ToString());

      if (space == null)
      {
        return new ApiResponse<List<FileItemDTO>>(
            false,
            "No se encontró espacio para el cliente",
            new List<FileItemDTO>());
      }
      
    
      var fileItemDTOs = _mapper.Map<List<FileItemDTO>>(space.FileResourcesCloud.ToList());
      if (fileItemDTOs == null || fileItemDTOs.Count == 0)
      {
        return new ApiResponse<List<FileItemDTO>>(
            true,
            "No se encontraron archivos para el cliente",
            new List<FileItemDTO>());
      }
      _logger.LogInformation(
          "Archivos obtenidos para cliente {UserId}: {FileCount} archivos",
          UserId,
          fileItemDTOs.Count);

      return new ApiResponse<List<FileItemDTO>>(
          true,
          $"{fileItemDTOs.Count} archivos encontrados",
          fileItemDTOs);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error al obtener archivos para cliente {UserId}", UserId);
      return new ApiResponse<List<FileItemDTO>>(
          false,
          "Error interno al obtener archivos",
          new List<FileItemDTO>());
    }
  }

    public async Task<ApiResponse<SpaceCloud>> GetAllSpaceAsync(Guid UserId, CancellationToken ct = default)
    {
        var space = await _context.SpacesClouds
          .AsNoTracking()
          .FirstOrDefaultAsync(s => s.UserId == UserId, ct);

          return space == null
            ? await Task.FromResult(new  ApiResponse<SpaceCloud>(false, "No se encontró espacio para el cliente", null))
            : await Task.FromResult(new ApiResponse<SpaceCloud>(true, "Espacio encontrado", space));

        throw new NotImplementedException();
    }
}
