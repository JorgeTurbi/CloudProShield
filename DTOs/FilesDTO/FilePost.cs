namespace DTOs.FilesDTO;

public class FilePost
{
    public required Guid CustomerId { get; set; }
    public required IFormFile File { get; set; }
    public string? CustomFolder { get; set; }
}
