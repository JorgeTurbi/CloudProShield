namespace CloudShield.DTOs.FileSystem;

public class UserFolderStructureDTO
{
    public Guid UserId { get; set; }
    public string Year { get; set; } = string.Empty;
    public List<FolderDTO> Folders { get; set; } = new List<FolderDTO>();
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public long UsedBytes { get; set; }
    public long MaxBytes { get; set; }
}
