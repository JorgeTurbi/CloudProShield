namespace CloudShield.DTOs.FileSystem;

public class FolderDTO
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }
}

public class NewFolderDTO
{
    public required string Name { get; set; }
}
