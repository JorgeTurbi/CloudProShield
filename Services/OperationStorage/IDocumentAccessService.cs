using DTOs.DocumentAccess;

namespace CloudShield.Services.OperationStorage;

public interface IDocumentAccessService
{
    Task PrepareSecureDocumentAccessAsync(
        Guid documentId,
        Guid signerId,
        string accessToken,
        string sessionId,
        string requestFingerprint,
        DateTime expiresAt,
        CancellationToken ct
    );

    Task<DocumentAccessResultDto> GetDocumentForSigningAsync(
        string accessToken,
        string sessionId,
        CancellationToken ct
    );

    Task<bool> ValidateAccessRequestAsync(
        string accessToken,
        string sessionId,
        string requestFingerprint,
        CancellationToken ct
    );
}
