using System.Security;
using CloudShield.Services.OperationStorage;
using RabbitMQ.Contracts.Events;
using RabbitMQ.Messaging;
using Services.SecurityService;

namespace RabbitMQ.Integration.Handlers;

public sealed class SecureDocumentAccessRequestedEventHandler
    : IIntegrationEventHandler<SecureDocumentAccessRequestedEvent>
{
    private readonly IDocumentAccessService _documentAccess;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<SecureDocumentAccessRequestedEventHandler> _log;

    public SecureDocumentAccessRequestedEventHandler(
        IDocumentAccessService documentAccess,
        ILogger<SecureDocumentAccessRequestedEventHandler> log,
        IEncryptionService encryption
    )
    {
        _documentAccess = documentAccess;
        _log = log;
        _encryption = encryption;
    }

    public async Task HandleAsync(SecureDocumentAccessRequestedEvent e, CancellationToken ct)
    {
        try
        {
            _log.LogInformation(
                "Procesando SecureDocumentAccessRequestedEvent para documento {DocumentId}",
                e.DocumentId
            );

            // Descifrar payload sensible
            DocumentAccessPayload payload;
            try
            {
                payload = _encryption.Decrypt<DocumentAccessPayload>(e.EncryptedPayload);
            }
            catch (Exception ex)
            {
                _log.LogError(
                    ex,
                    "Error descifrando payload para documento {DocumentId}",
                    e.DocumentId
                );
                throw new SecurityException("Payload corrupto o clave incorrecta");
            }

            // Verificar integridad del payload
            if (!VerifyPayloadIntegrity(payload, e.PayloadHash))
            {
                _log.LogWarning(
                    "Integridad del payload comprometida para documento {DocumentId}",
                    e.DocumentId
                );
                throw new SecurityException("Integridad del payload comprometida");
            }

            // Verificar expiración
            if (e.ExpiresAt < DateTime.UtcNow)
            {
                _log.LogWarning("Evento expirado para documento {DocumentId}", e.DocumentId);
                return;
            }

            // Preparar acceso seguro al documento
            await _documentAccess.PrepareSecureDocumentAccessAsync(
                e.DocumentId,
                payload.SignerId,
                payload.AccessToken,
                payload.SessionId,
                payload.RequestFingerprint,
                e.ExpiresAt,
                ct
            );

            _log.LogInformation(
                "SecureDocumentAccessRequestedEvent procesado exitosamente → {DocumentId}",
                e.DocumentId
            );
        }
        catch (SecurityException)
        {
            // Re-lanzar excepciones de seguridad
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error procesando SecureDocumentAccessRequestedEvent para documento {DocumentId}",
                e.DocumentId
            );
            throw;
        }
    }

    private bool VerifyPayloadIntegrity(DocumentAccessPayload payload, string expectedHash)
    {
        try
        {
            var data =
                $"{payload.SignerId}:{payload.AccessToken}:{payload.SessionId}:{payload.RequestFingerprint}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            var computedHash = Convert.ToHexString(hash);

            return string.Equals(computedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
