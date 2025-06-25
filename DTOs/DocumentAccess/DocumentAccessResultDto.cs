namespace DTOs.DocumentAccess;

public class DocumentAccessResultDto
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string SessionId { get; set; } = string.Empty;
}
