namespace CloudShield.DTOs.FileSystem;

public class FolderContentDTO
{
  public string FolderName { get; set; } = string.Empty;
  public string FolderPath { get; set; } = string.Empty;
  public List<FileItemDTO> Files { get; set; } = new List<FileItemDTO>();
  public int TotalFiles { get; set; }
  public long TotalSizeBytes { get; set; }
}
