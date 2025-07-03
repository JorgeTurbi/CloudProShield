namespace RabbitMQ.Contracts.Events;

/// <summary>
/// Evento publicado cuando un documento ha sido completamente firmado por todas las partes
/// y sellado digitalmente con el certificado de la plataforma.
/// </summary>
public sealed record DocumentSealedEvent
{
    public Guid Id { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid SignatureRequestId { get; init; }
    public Guid OriginalDocumentId { get; init; }
    public Guid SealedDocumentId { get; init; } // ID del nuevo recurso de archivo sellado
    public string SealedDocumentRelativePath { get; init; } = default!;
    public IReadOnlyList<string> SignerEmails { get; init; } = default!;
}
