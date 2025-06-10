using CloudShield.DTOs.FileSystem;
using CloudShield.Entities.Operations;
using Commons;

namespace CloudShield.Services.FileSystemServices;

public interface IFileSystemReadServiceUser
{
    /// <summary>
    /// Obtiene la estructura completa de carpetas de un cliente
    /// </summary>
    /// <param name="UserId">ID del cliente</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Estructura de carpetas del cliente</returns>
    Task<ApiResponse<UserFolderStructureDTO>> GetCustomerFolderStructureAsync(
        Guid UserId,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene el contenido de una carpeta específica
    /// </summary>
    /// <param name="UserId">ID del cliente</param>
    /// <param name="folderName">Nombre de la carpeta (Firms o Documents)</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Contenido de la carpeta</returns>
    Task<ApiResponse<FolderContentDTO>> GetFolderContentAsync(
        Guid UserId,
        string folderName,
        CancellationToken ct = default);
/// <summary>
    /// Lista todas las carpetas disponibles para un cliente
    /// </summary>
    /// <param name="UserId">ID del cliente</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Lista de carpetas</returns>
    /// 

 Task<ApiResponse<FolderContentDTO>> GetFolderContentExploreAsync(Guid UserId ,CancellationToken ct = default);
    /// <summary>
    /// Lista todas las carpetas disponibles para un cliente
    /// </summary>
    /// <param name="UserId">ID del cliente</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Lista de carpetas</returns>
    Task<ApiResponse<List<FolderDTO>>> GetCustomerFoldersAsync(
        Guid UserId,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene todos los archivos de un cliente independientemente de la carpeta
    /// </summary>
    /// <param name="UserId">ID del cliente</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Lista de todos los archivos del cliente</returns>
    Task<ApiResponse<List<FileItemDTO>>> GetAllCustomerFilesAsync(Guid UserId, CancellationToken ct = default);
    Task<ApiResponse<SpaceCloud>> GetAllSpaceAsync(Guid UserId, CancellationToken ct = default);
    


}
