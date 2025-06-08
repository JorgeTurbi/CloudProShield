namespace CloudShield.DTOs.FileSystem;

public class FileItemDTO
{
  public Guid Id { get; set; }
  public string FileName { get; set; } = string.Empty;
  public string ContentType { get; set; } = string.Empty;
  public string RelativePath { get; set; } = string.Empty;
  public long SizeBytes { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public string Category { get; set; } = string.Empty;
}
