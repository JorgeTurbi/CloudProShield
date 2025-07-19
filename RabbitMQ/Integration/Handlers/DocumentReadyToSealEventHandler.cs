using System;
using System.Security.Cryptography;
using System.Text;
using CloudShield.Entities.Operations;
using CloudShield.Services.OperationStorage;
using CloudShield.Services.PdfSealing;
using DataContext; // <-- AÑADIR ESTE USING PARA ACCEDER AL DBCONTEXT
using Microsoft.EntityFrameworkCore; // <-- AÑADIR ESTE USING PARA EL MÉTODO 'INCLUDE'
using RabbitMQ.Contracts.Events;
using RabbitMQ.Messaging;
using Services.SecurityService;

namespace RabbitMQ.Integration.Handlers;

public sealed class DocumentReadyToSealEventHandler
    : IIntegrationEventHandler<DocumentReadyToSealEvent>
{
    private readonly IStorageService _storage;
    private readonly IPdfSealingService _pdfSealer;
    private readonly IEncryptionService _encryption;
    private readonly IDocumentAccessService _documentAccess;
    private readonly IEventBus _bus;
    private readonly ILogger<DocumentReadyToSealEventHandler> _log;
    private readonly ApplicationDbContext _db;

    public DocumentReadyToSealEventHandler(
        IStorageService storage,
        IPdfSealingService pdfSealer,
        IEventBus bus,
        ILogger<DocumentReadyToSealEventHandler> log,
        ApplicationDbContext db,
        IEncryptionService encryption,
        IDocumentAccessService documentAccess
    ) // <-- INYECTAR EL DBCONTEXT
    {
        _storage = storage;
        _pdfSealer = pdfSealer;
        _bus = bus;
        _log = log;
        _db = db; // <-- ASIGNAR EL DBCONTEXT
        _encryption = encryption;
        _documentAccess = documentAccess;
    }

    public async Task HandleAsync(DocumentReadyToSealEvent e, CancellationToken ct)
    {
        _log.LogInformation(
            "Iniciando proceso de sellado para DocumentId: {DocumentId}",
            e.DocumentId
        );

        // 1. Obtener el metadato del documento original y su 'Space' para saber a qué cliente pertenece
        // CORRECCIÓN: Usamos el DbContext para hacer un 'Include' y traer la entidad 'Space' relacionada.
        FileResource? original = await _db
            .FileResources.AsNoTracking()
            .Include(fr => fr.Space) // <-- CARGAMOS LA ENTIDAD 'SPACE'
            .FirstOrDefaultAsync(fr => fr.Id == e.DocumentId, ct);

        if (original == null || original.Space == null)
        {
            _log.LogError(
                "No se encontró el metadato del documento original con ID: {DocumentId} o su Space asociado.",
                e.DocumentId
            );
            return;
        }

        // CORRECCIÓN: Obtenemos el CustomerId desde la entidad Space navegada.
        var uploaderCustomerId = original.Space.CustomerId;

        // 2. Obtener el stream del documento original
        (bool ok, Stream? pdfStream, _, string? reason) = await _storage.GetFileAsync(
            uploaderCustomerId,
            original.RelativePath,
            ct
        );

        if (!ok || pdfStream == null)
        {
            _log.LogError("No se pudo leer PDF original: {Reason}", reason);
            return;
        }

        await using (pdfStream)
        {
            try
            {
                /* 3 ▸ estampar firmas + sello LTV */
                using var sealedStream = await _pdfSealer.ApplySignaturesAndSealAsync(
                    pdfStream,
                    e.Signatures
                );

                byte[] sealedBytes = sealedStream.ToArray(); // buffer para todos
                string baseName = Path.GetFileNameWithoutExtension(original.FileName);
                string sealedName = $"{baseName}-signed-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

                /* 4 ▸ guardar en uploader y cada firmante  */
                string? uploaderRel = await SaveForCustomerAsync(
                    uploaderCustomerId,
                    sealedBytes,
                    sealedName,
                    ct
                );

                foreach (var cid in e.Signatures.Select(s => s.CustomerId).Distinct())
                {
                    if (cid != uploaderCustomerId)
                        await SaveForCustomerAsync(cid, sealedBytes, sealedName, ct);
                }

                if (uploaderRel is null)
                {
                    _log.LogError("No se pudo guardar el PDF sellado para el uploader");
                    return;
                }

                /* 5 ▸ leer metadato del archivo recién guardado (uploader) */
                var sealedMeta = await _storage.FindMetaAsync(uploaderCustomerId, uploaderRel, ct);
                if (sealedMeta is null)
                {
                    _log.LogError("Metadato no encontrado para {Path}", uploaderRel);
                    return;
                }

                foreach (var signerGroup in e.Signatures.GroupBy(s => (s.SignerId, s.SignerEmail)))
                {
                    try
                    {
                        var signerId = signerGroup.Key.SignerId;
                        var signerEmail = signerGroup.Key.SignerEmail;

                        // 1. Generar credenciales de acceso
                        var accessToken = Guid.NewGuid().ToString("N");
                        var sessionId = GenerateRandomHex(16); // helper
                        var requestFingerprint = GenerateFingerprint(
                            sealedMeta.Id,
                            signerId,
                            sessionId
                        );
                        var expiresAt = DateTime.UtcNow.AddDays(7);

                        var payload = new DocumentAccessPayload
                        {
                            SignerId = signerId,
                            SignerEmail = signerEmail,
                            AccessToken = accessToken,
                            SessionId = sessionId,
                            RequestFingerprint = requestFingerprint,
                        };

                        var encrypted = _encryption.Encrypt(payload);
                        var hash = ComputePayloadHash(payload);

                        // 2. Preparar acceso local (cache)
                        await _documentAccess.PrepareSecureDocumentAccessAsync(
                            sealedMeta.Id,
                            signerId,
                            accessToken,
                            sessionId,
                            requestFingerprint,
                            expiresAt,
                            ct
                        );

                        // 3. Publicar evento seguro para email
                        var evAccess = new SecureDownloadSignedDocument(
                            Id: Guid.NewGuid(),
                            OccurredOn: DateTime.UtcNow,
                            SealedDocumentId: sealedMeta.Id,
                            EncryptedPayload: encrypted,
                            PayloadHash: hash,
                            ExpiresAt: expiresAt
                        );

                        _bus.Publish(nameof(SecureDownloadSignedDocument), evAccess);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(
                            ex,
                            "No se pudo preparar acceso para {Email}",
                            signerGroup.Key.SignerEmail
                        );
                    }
                }

                /* 6 ▸ publicar evento DocumentSealed */
                var ev = new DocumentSealedEvent
                {
                    Id = Guid.NewGuid(),
                    OccurredOn = DateTime.UtcNow,
                    SignatureRequestId = e.SignatureRequestId,
                    OriginalDocumentId = e.DocumentId,
                    SealedDocumentId = sealedMeta.Id,
                    SealedDocumentRelativePath = sealedMeta.RelativePath,
                    SignerEmails = e.Signatures.Select(s => s.SignerEmail).ToList(),
                };

                _bus.Publish(nameof(DocumentSealedEvent), ev);
                _log.LogInformation("Documento sellado almacenado en {Path}", uploaderRel);
            }
            catch (Exception ex)
            {
                _log.LogCritical(ex, "Fallo crítico sellando Doc {Doc}", e.DocumentId);
                throw;
            }
        }
    }

    /* ───────── helper ───────── */
    async Task<string?> SaveForCustomerAsync(
        Guid customerId,
        byte[] pdfBytes,
        string fileName,
        CancellationToken ct
    )
    {
        await using var ms = new MemoryStream(pdfBytes, writable: false);
        var form = new FormFile(ms, 0, ms.Length, "pdf", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf",
        };

        (bool ok, string? rel) = await _storage.SaveFileAsync(customerId, form, ct, "Firms");

        if (ok)
            _log.LogInformation("PDF guardado para cliente {Cid} en {Rel}", customerId, rel);
        else
            _log.LogError("No se pudo guardar PDF para cliente {Cid}", customerId);

        return ok ? rel : null;
    }

    private static string GenerateRandomHex(int bytesLen)
    {
        var bytes = new byte[bytesLen];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateFingerprint(Guid sealedDocId, Guid signerId, string sessionId)
    {
        var data = $"{sealedDocId}:{signerId}:{sessionId}:{DateTime.UtcNow:yyyyMMddHHmmss}";
        using var sha = SHA256.Create();
        return Convert
            .ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(data)))[..16]
            .ToLowerInvariant();
    }

    private static string ComputePayloadHash(DocumentAccessPayload p)
    {
        var data = $"{p.SignerId}:{p.AccessToken}:{p.SessionId}:{p.RequestFingerprint}";
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }
}
