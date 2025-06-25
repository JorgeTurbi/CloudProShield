using DTOs.DocumentAccess;

namespace CloudShield.Services.OperationStorage;

public interface IDocumentAccessService
{
    Task PrepareDocumentAccessAsync(
        Guid documentId,
        Guid signerId,
        string accessToken,
        string sessionId,
        DateTime expiresAt,
        CancellationToken ct
    );

    Task<DocumentAccessResultDto> GetDocumentForSigningAsync(
        string accessToken,
        string sessionId,
        CancellationToken ct
    );
}
