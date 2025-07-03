using RabbitMQ.Contracts.Events;

namespace CloudShield.Services.PdfSealing;

public interface IPdfSealingService
{
    /// <summary>
    /// Aplica las firmas y un sello digital a un documento PDF.
    /// </summary>
    /// <param name="originalDocumentStream">Stream del documento original.</param>
    /// <param name="signatures">Lista de información de las firmas a aplicar.</param>
    /// <returns>Un stream de memoria con el contenido del PDF sellado.</returns>
    Task<MemoryStream> ApplySignaturesAndSealAsync(
        Stream originalDocumentStream,
        IReadOnlyList<SignedImageDto> signatures
    );
}
