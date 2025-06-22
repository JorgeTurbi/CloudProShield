namespace CloudShield.DTOs.FileSystem;

public class FileResourceCloudDTO
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; }
    public DateTime CreatedAt { get; set; }
    // NO incluir SpaceCloud aquí para evitar el ciclo
}
