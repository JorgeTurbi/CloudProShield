using CloudShield.Entities.Operations;

namespace DTOs.DocumentAccess;

public class DocumentAccessInfo
{
    public Guid DocumentId { get; set; }
    public Guid SignerId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public FileResource Document { get; set; } = default!;
}
