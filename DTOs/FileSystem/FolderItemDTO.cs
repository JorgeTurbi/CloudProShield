using CloudShield.DTOs.FileSystem;

public class FolderItemDTO
{
    public string Name { get; set; }
    public string RelativePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FolderContentDTO
{
    public string FolderName { get; set; }
    public string FolderPath { get; set; }
    public List<FolderItemDTO> Folders { get; set; } = new List<FolderItemDTO>();
    public List<FolderDTO> SubFolders { get; set; } = new();
    public List<FileItemDTO> Files { get; set; } = new List<FileItemDTO>();
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
}
