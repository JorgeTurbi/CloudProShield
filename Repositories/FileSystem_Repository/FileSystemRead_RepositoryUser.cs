using AutoMapper;
using CloudShield.Commons.Helpers;
using CloudShield.DTOs.FileSystem;
using CloudShield.Entities.Operations;
using CloudShield.Services.FileSystemServices;
using Commons;
using DataContext;
using Microsoft.AspNetCore.Http;
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
    _rootPath = _configuration["Storage:RootPath"] ?? "StorageCloud";
  }

  public async Task<ApiResponse<UserFolderStructureDTO>> GetUserFolderStructureAsync(
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

      var userRoot = FileStoragePathResolver.UserRoot(_rootPath, UserId);
      var dto = new UserFolderStructureDTO
      {
        UserId = UserId,
        Year = DateTime.UtcNow.Year.ToString(),
        UsedBytes = space.UsedBytes,
        MaxBytes = space.MaxBytes,
        TotalFiles = space.FileResourcesCloud.Count,
        TotalSizeBytes = space.FileResourcesCloud.Sum(f => f.SizeBytes),
        Folders = Directory.Exists(userRoot)
              ? Directory.GetDirectories(userRoot)
                  .Select(dir =>
                  {
                    var name = Path.GetFileName(dir);
                    var fIn = space.FileResourcesCloud
                          .Where(f => f.RelativePath.StartsWith(name + "/")).ToList();

                    return new FolderDTO
                    {
                      Name = name,
                      FullPath = dir,
                      CreatedAt = Directory.GetCreationTime(dir),
                      FileCount = fIn.Count,
                      TotalSizeBytes = fIn.Sum(f => f.SizeBytes)
                    };
                  }).ToList()
              : new()
      };

      _logger.LogInformation(
          "Estructura de carpetas obtenida para cliente {UserId}: {FolderCount} carpetas",
          UserId,
          dto.Folders.Count);

      return new ApiResponse<UserFolderStructureDTO>(
          true,
          $"Estructura obtenida correctamente. {dto.Folders.Count} carpetas encontradas",
          dto);
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
    Guid userId, string folderPath, CancellationToken ct = default)
  {
    folderPath = folderPath.Trim('/');               // “Fotos/2025”
    var year = DateTime.UtcNow.Year.ToString();
    var basePath = Path.Combine(_rootPath, year, userId.ToString("N"), folderPath);

    /* ---------- sub-carpetas físicas ---------- */
    var subDirs = Directory.Exists(basePath)
        ? Directory.GetDirectories(basePath)
        : Array.Empty<string>();

    /* ---------- archivos SÓLO de este nivel ---------- */
    var space = await _context.SpacesClouds
        .AsNoTracking()
        .Include(s => s.FileResourcesCloud)
        .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    var levelDir = folderPath.Replace('\\', '/');
    var filesHere = space?.FileResourcesCloud
        .Where(f => Path.GetDirectoryName(f.RelativePath)!
                    .Replace('\\', '/') == levelDir)
        .ToList() ?? new();

    var dto = new FolderContentDTO
    {
      FolderName = Path.GetFileName(folderPath),
      FolderPath = $"{year}/{userId:N}/{folderPath}",
      SubFolders = subDirs.Select(d => new FolderDTO
      {
        Name = Path.GetFileName(d),
        FullPath = d,
        CreatedAt = Directory.GetCreationTime(d),
        FileCount = space.FileResourcesCloud.Count(fr =>
             fr.RelativePath.StartsWith($"{folderPath.TrimEnd('/')}/{Path.GetFileName(d)}/")),
        TotalSizeBytes = space.FileResourcesCloud
               .Where(fr => fr.RelativePath.StartsWith($"{folderPath.TrimEnd('/')}/{Path.GetFileName(d)}/"))
               .Sum(fr => fr.SizeBytes)
      }).ToList(),
      Files = _mapper.Map<List<FileItemDTO>>(filesHere),
      TotalFiles = filesHere.Count,
      TotalSizeBytes = filesHere.Sum(f => f.SizeBytes)
    };

    return new(true, "ok", dto);
  }

  public async Task<ApiResponse<List<FolderDTO>>> GetUserFoldersAsync(
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
      var userPath = FileStoragePathResolver.UserRoot(_rootPath, UserId);

      var folders = new List<FolderDTO>();

      if (Directory.Exists(userPath))
      {
        var subDirectories = Directory.GetDirectories(userPath);

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

  public async Task<ApiResponse<List<FileItemDTO>>> GetAllUserFilesAsync(
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

  public async Task<ApiResponse<SpaceCloud>> GetAllSpaceAsync(string guid, CancellationToken ct = default)
  {
    if (Guid.TryParse(guid, out Guid UserId))
    {
      // El GUID es válido, puedes usar la variable 'guid' aquí
    }
    else
    {
      // El string no es un GUID válido
      Console.WriteLine("El formato del GUID no es válido.");
    }
    var space = await _context.SpacesClouds
      .AsNoTracking()
      .FirstOrDefaultAsync(s => s.UserId == UserId, ct);

    return space == null
      ? await Task.FromResult(new ApiResponse<SpaceCloud>(false, "No se encontró espacio para el cliente", null))
      : await Task.FromResult(new ApiResponse<SpaceCloud>(true, "Espacio encontrado", space));

    throw new NotImplementedException();
  }

  public async Task<ApiResponse<FolderContentDTO>> GetFolderContentExploreAsync(Guid UserId, CancellationToken ct = default)
  {
    try
    {
      var space = await _context.SpacesClouds
          .AsNoTracking()
          .Include(s => s.FileResourcesCloud)
          .FirstOrDefaultAsync(s => s.UserId == UserId, ct);

      _logger.LogInformation(
          "Explorando contenido de carpeta para usuario {UserId}",
          UserId);

      if (space == null)
      {
        return new ApiResponse<FolderContentDTO>(
            false,
            "No se encontró espacio para el usuario",
            null);
      }

      // Construir la ruta base de almacenamiento
      var userPath = FileStoragePathResolver.UserRoot(_rootPath, UserId);

      // Obtener carpetas (subcarpetas) directamente dentro de la ruta base
      var folderPaths = Directory.GetDirectories(userPath);
      var folderItems = folderPaths.Select(folderPath => new FolderItemDTO
      {
        Name = Path.GetFileName(folderPath),
        RelativePath = folderPath.Replace(_rootPath, "").TrimStart('/'),
        CreatedAt = Directory.GetCreationTime(folderPath),
        UpdatedAt = Directory.GetLastWriteTime(folderPath)
      }).ToList();

      // Obtener archivos que están dentro de esa ruta base
      var filesInFolder = space.FileResourcesCloud
       .Where(f => !f.RelativePath.Contains('/'))
       .ToList();

      var fileItems = filesInFolder.Select(f => new FileItemDTO
      {
        Id = f.Id,
        FileName = f.FileName,
        ContentType = f.ContentType,
        RelativePath = f.RelativePath,
        SizeBytes = f.SizeBytes,
        CreatedAt = f.CreateAt,
        UpdatedAt = f.UpdateAt,
        Category = "" // o asigna si tienes esta propiedad en la BD
      }).ToList();

      // Construir respuesta final
      var folderContent = new FolderContentDTO
      {
        FolderName = "root",
        FolderPath = userPath,
        Folders = folderItems,
        Files = fileItems,
        TotalFiles = fileItems.Count,
        TotalSizeBytes = fileItems.Sum(f => f.SizeBytes)
      };

      return new ApiResponse<FolderContentDTO>(
          true,
          "Contenido de carpeta explorado correctamente",
          folderContent);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error al obtener contenido de carpeta para usuario {UserId}", UserId);
      return new ApiResponse<FolderContentDTO>(
          false,
          "Error interno al obtener contenido de carpeta",
          null);
    }
  }

}
