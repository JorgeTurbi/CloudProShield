namespace CloudShield.DTOs.FileSystem;

public class SpaceCloudDTO
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public long MaxBytes { get; set; }
    public long UsedBytes { get; set; }
    public byte[] RowVersion { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<FileResourceCloudDTO> Files { get; set; } = new();
}
