namespace CloudShield.Commons.Helpers;

public static class FileStoragePathResolver
{
    /// raíz para archivos de CLIENTES (usa año)
    public static string CustomerRoot(string root, Guid customerId) =>
        Path.Combine(root, DateTime.UtcNow.Year.ToString(), customerId.ToString("N"));

    /// raíz para archivos de USUARIOS del sistema (sin año)
    public static string UserRoot(string root, Guid userId) =>
          Path.Combine(root, DateTime.UtcNow.Year.ToString(), userId.ToString("N"));
}