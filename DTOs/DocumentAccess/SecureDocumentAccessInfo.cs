using CloudShield.Entities.Operations;

namespace DTOs.DocumentAccess;

public class SecureDocumentAccessInfo
{
    public Guid DocumentId { get; set; }
    public Guid SignerId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public FileResource Document { get; set; } = default!;
    // public int AccessCount { get; set; }
    // public int MaxAccessCount { get; set; } = 5;
}
