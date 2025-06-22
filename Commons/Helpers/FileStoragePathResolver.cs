namespace CloudShield.Commons.Helpers;

public static class FileStoragePathResolver
{
    /// raíz para archivos de CLIENTES (usa año)
    public static string CustomerRoot(string root, Guid customerId) =>
        Path.Combine(root, DateTime.UtcNow.Year.ToString(), customerId.ToString("N"));

    /// <summary>
    /// Root path for system user files (NO YEAR - direct under StorageCloud)
    /// </summary>
    public static string UserRoot(string root, Guid userId) =>
        Path.Combine(root, userId.ToString("N"));

    /// <summary>
    /// Normalizes path separators and removes dangerous characters
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.Replace('\\', '/')
            .Trim('/')
            .Replace("//", "/")
            .Replace("../", "") // Security: prevent directory traversal
            .Replace("..\\", ""); // Security: prevent directory traversal
    }

    /// <summary>
    /// Combines user root with relative path safely
    /// </summary>
    public static string GetFullPath(string root, Guid userId, string relativePath)
    {
        var userRoot = UserRoot(root, userId);
        var normalizedRelative = NormalizePath(relativePath);

        return string.IsNullOrEmpty(normalizedRelative)
            ? userRoot
            : Path.Combine(userRoot, normalizedRelative.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>
    /// Gets the parent directory path from a relative path
    /// </summary>
    public static string GetParentPath(string relativePath)
    {
        var normalized = NormalizePath(relativePath);
        if (string.IsNullOrEmpty(normalized))
            return string.Empty;

        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash > 0 ? normalized[..lastSlash] : string.Empty;
    }

    /// <summary>
    /// Checks if a path is a direct child of another path
    /// </summary>
    public static bool IsDirectChild(string parentPath, string childPath)
    {
        var normalizedParent = NormalizePath(parentPath);
        var normalizedChild = NormalizePath(childPath);

        if (string.IsNullOrEmpty(normalizedParent))
            return !normalizedChild.Contains('/');

        if (!normalizedChild.StartsWith(normalizedParent + "/"))
            return false;

        var remainingPath = normalizedChild.Substring(normalizedParent.Length + 1);
        return !remainingPath.Contains('/');
    }

    /// <summary>
    /// Gets all parent paths for a given path (for breadcrumb navigation)
    /// </summary>
    public static List<string> GetParentPaths(string relativePath)
    {
        var normalized = NormalizePath(relativePath);
        var parents = new List<string>();

        if (string.IsNullOrEmpty(normalized))
            return parents;

        var parts = normalized.Split('/');
        var currentPath = string.Empty;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            currentPath = string.IsNullOrEmpty(currentPath)
                ? parts[i]
                : $"{currentPath}/{parts[i]}";
            parents.Add(currentPath);
        }

        return parents;
    }

    /// <summary>
    /// Builds relative path safely from components
    /// </summary>
    public static string BuildRelativePath(params string[] pathComponents)
    {
        if (pathComponents == null || pathComponents.Length == 0)
            return string.Empty;

        var cleanComponents = pathComponents
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(NormalizePath)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        return string.Join("/", cleanComponents);
    }

    /// <summary>
    /// Checks if a file belongs to a specific folder (including subfolders)
    /// </summary>
    public static bool IsFileInFolder(string filePath, string folderPath)
    {
        var normalizedFile = NormalizePath(filePath);
        var normalizedFolder = NormalizePath(folderPath);

        if (string.IsNullOrEmpty(normalizedFolder))
            return true; // Root folder contains everything

        return normalizedFile.StartsWith(
            normalizedFolder + "/",
            StringComparison.OrdinalIgnoreCase
        );
    }

    /// <summary>
    /// Checks if a file is directly in a folder (not in subfolders)
    /// </summary>
    public static bool IsFileDirectlyInFolder(string filePath, string folderPath)
    {
        var normalizedFile = NormalizePath(filePath);
        var normalizedFolder = NormalizePath(folderPath);

        if (string.IsNullOrEmpty(normalizedFolder))
        {
            // Root level - files with no path separators
            return !normalizedFile.Contains('/');
        }

        // Check if file starts with folder path
        if (!normalizedFile.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase))
            return false;

        // Get the remaining path after the folder
        var remainingPath = normalizedFile[(normalizedFolder.Length + 1)..];

        // Check if there are no more path separators (direct child)
        return !remainingPath.Contains('/');
    }
}
