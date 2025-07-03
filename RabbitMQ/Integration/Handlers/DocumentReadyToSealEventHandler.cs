using CloudShield.Entities.Operations;
using CloudShield.Services.OperationStorage;
using CloudShield.Services.PdfSealing;
using DataContext; // <-- AÑADIR ESTE USING PARA ACCEDER AL DBCONTEXT
using Microsoft.EntityFrameworkCore; // <-- AÑADIR ESTE USING PARA EL MÉTODO 'INCLUDE'
using RabbitMQ.Contracts.Events;
using RabbitMQ.Messaging;

namespace RabbitMQ.Integration.Handlers;

public sealed class DocumentReadyToSealEventHandler
    : IIntegrationEventHandler<DocumentReadyToSealEvent>
{
    private readonly IStorageService _storage;
    private readonly IPdfSealingService _pdfSealer;
    private readonly IEventBus _bus;
    private readonly ILogger<DocumentReadyToSealEventHandler> _log;
    private readonly ApplicationDbContext _db; // <-- INYECTAR EL DBCONTEXT

    public DocumentReadyToSealEventHandler(
        IStorageService storage,
        IPdfSealingService pdfSealer,
        IEventBus bus,
        ILogger<DocumentReadyToSealEventHandler> log,
        ApplicationDbContext db
    ) // <-- INYECTAR EL DBCONTEXT
    {
        _storage = storage;
        _pdfSealer = pdfSealer;
        _bus = bus;
        _log = log;
        _db = db; // <-- ASIGNAR EL DBCONTEXT
    }

    public async Task HandleAsync(DocumentReadyToSealEvent e, CancellationToken ct)
    {
        _log.LogInformation(
            "Iniciando proceso de sellado para DocumentId: {DocumentId}",
            e.DocumentId
        );

        // 1. Obtener el metadato del documento original y su 'Space' para saber a qué cliente pertenece
        // CORRECCIÓN: Usamos el DbContext para hacer un 'Include' y traer la entidad 'Space' relacionada.
        FileResource? originalFileMeta = await _db
            .FileResources.AsNoTracking()
            .Include(fr => fr.Space) // <-- CARGAMOS LA ENTIDAD 'SPACE'
            .FirstOrDefaultAsync(fr => fr.Id == e.DocumentId, ct);

        if (originalFileMeta == null || originalFileMeta.Space == null)
        {
            _log.LogError(
                "No se encontró el metadato del documento original con ID: {DocumentId} o su Space asociado.",
                e.DocumentId
            );
            return;
        }

        // CORRECCIÓN: Obtenemos el CustomerId desde la entidad Space navegada.
        var customerId = originalFileMeta.Space.CustomerId;

        // 2. Obtener el stream del documento original
        (bool ok, Stream? contentStream, string? contentType, string? reason) =
            await _storage.GetFileAsync(customerId, originalFileMeta.RelativePath, ct);
        if (!ok || contentStream == null)
        {
            _log.LogError(
                "No se pudo obtener el archivo original {Path}: {Reason}",
                originalFileMeta.RelativePath,
                reason
            );
            return;
        }

        using (contentStream)
        {
            try
            {
                // 3. Aplicar firmas y sellar el documento
                using var sealedPdfStream = await _pdfSealer.ApplySignaturesAndSealAsync(
                    contentStream,
                    e.Signatures
                );

                // 4. Guardar el nuevo documento sellado
                var originalFileName = Path.GetFileNameWithoutExtension(originalFileMeta.FileName);
                var sealedFileName =
                    $"{originalFileName}-signed-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

                var formFile = new FormFile(
                    sealedPdfStream,
                    0,
                    sealedPdfStream.Length,
                    "pdf",
                    sealedFileName
                )
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/pdf",
                };

                // Guardamos en una carpeta dedicada para documentos firmados
                (bool savedOk, string? savedPathOrReason) = await _storage.SaveFileAsync(
                    customerId, // Usamos la variable que obtuvimos
                    formFile,
                    ct,
                    "SignedDocuments"
                );

                if (!savedOk)
                {
                    _log.LogError(
                        "Error al guardar el documento sellado: {Reason}",
                        savedPathOrReason
                    );
                    return;
                }

                // 5. Obtener el metadato del nuevo archivo para tener su ID
                var sealedFileMeta = await _storage.FindMetaAsync(
                    customerId,
                    savedPathOrReason!,
                    ct
                );
                if (sealedFileMeta == null)
                {
                    _log.LogError(
                        "No se encontró el metadato del archivo recién guardado en {Path}",
                        savedPathOrReason
                    );
                    return;
                }

                // 6. Publicar evento de éxito
                var successEvent = new DocumentSealedEvent
                {
                    Id = Guid.NewGuid(),
                    OccurredOn = DateTime.UtcNow,
                    SignatureRequestId = e.SignatureRequestId,
                    OriginalDocumentId = e.DocumentId,
                    SealedDocumentId = sealedFileMeta.Id,
                    SealedDocumentRelativePath = sealedFileMeta.RelativePath,
                    SignerEmails = e.Signatures.Select(s => s.SignerEmail).ToList(),
                };

                _bus.Publish("DocumentSealedEvent", successEvent);

                _log.LogInformation(
                    "Documento {DocumentId} sellado y guardado exitosamente en {Path}",
                    e.DocumentId,
                    savedPathOrReason
                );
            }
            catch (Exception ex)
            {
                _log.LogCritical(
                    ex,
                    "Fallo crítico en el proceso de sellado para DocumentId: {DocumentId}",
                    e.DocumentId
                );
                throw;
            }
        }
    }
}
