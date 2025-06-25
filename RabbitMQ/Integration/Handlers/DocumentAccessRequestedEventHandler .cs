using CloudShield.Services.OperationStorage;
using RabbitMQ.Contracts.Events;
using RabbitMQ.Messaging;

namespace RabbitMQ.Integration.Handlers;

public sealed class DocumentAccessRequestedEventHandler
    : IIntegrationEventHandler<DocumentAccessRequestedEvent>
{
    private readonly IDocumentAccessService _documentAccess;
    private readonly ILogger<DocumentAccessRequestedEventHandler> _log;

    public DocumentAccessRequestedEventHandler(
        IDocumentAccessService documentAccess,
        ILogger<DocumentAccessRequestedEventHandler> log
    )
    {
        _documentAccess = documentAccess;
        _log = log;
    }

    public async Task HandleAsync(DocumentAccessRequestedEvent e, CancellationToken ct)
    {
        try
        {
            _log.LogInformation(
                "Procesando DocumentAccessRequestedEvent para documento {DocumentId}, signer {SignerId}, session {SessionId}",
                e.DocumentId,
                e.SignerId,
                e.SessionId
            );

            // Preparar acceso temporal al documento
            await _documentAccess.PrepareDocumentAccessAsync(
                e.DocumentId,
                e.SignerId,
                e.AccessToken,
                e.SessionId,
                e.ExpiresAt,
                ct
            );

            _log.LogInformation(
                "DocumentAccessRequestedEvent procesado exitosamente → {DocumentId}",
                e.DocumentId
            );
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error procesando DocumentAccessRequestedEvent para documento {DocumentId}",
                e.DocumentId
            );
            throw;
        }
    }
}
