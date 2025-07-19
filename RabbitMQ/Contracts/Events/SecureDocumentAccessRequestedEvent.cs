namespace RabbitMQ.Contracts.Events;

public sealed record SecureDocumentAccessRequestedEvent
{
    public Guid Id { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid DocumentId { get; init; }
    public string EncryptedPayload { get; init; } = default!; // Datos sensibles cifrados
    public string PayloadHash { get; init; } = default!; // Hash para verificar integridad
    public DateTime ExpiresAt { get; init; }
}

public sealed record SecureDownloadSignedDocument(
    Guid Id,
    DateTime OccurredOn,
    Guid SealedDocumentId,
    string EncryptedPayload,
    string PayloadHash,
    DateTime ExpiresAt
);

// Payload que se cifra
public sealed record DocumentAccessPayload
{
    public Guid SignerId { get; init; }
    public string SignerEmail { get; init; } = default!;
    public string AccessToken { get; init; } = default!;
    public string SessionId { get; init; } = default!;
    public string RequestFingerprint { get; init; } = default!; // Huella única de la solicitud
}
