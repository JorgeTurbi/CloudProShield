using System.Security.Cryptography.X509Certificates;
using CloudShield.Services.PdfSealing;
using iText.Forms;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Signatures;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using RabbitMQ.Contracts.Events;
// AÑADIR USINGS PARA IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using ITextImage = iText.Layout.Element.Image; // Alias para evitar ambigüedad

public class PdfSealingService : IPdfSealingService
{
    private readonly IConfiguration _config;
    private readonly ILogger<PdfSealingService> _log;

    public PdfSealingService(IConfiguration config, ILogger<PdfSealingService> log)
    {
        _config = config;
        _log = log;
    }

    public async Task<MemoryStream> ApplySignaturesAndSealAsync(
        Stream originalDocumentStream,
        IReadOnlyList<SignedImageDto> signatures
    )
    {
        var outputStream = new MemoryStream();
        using var intermediateStream = new MemoryStream();

        PdfDocument pdfDoc = null!;
        try
        {
            var pdfReader = new PdfReader(originalDocumentStream);
            var pdfWriter = new PdfWriter(intermediateStream);
            pdfDoc = new PdfDocument(pdfReader, pdfWriter);
            var document = new Document(pdfDoc);

            foreach (var signature in signatures)
            {
                // 1. Limpiar el string Base64
                var base64Data = signature.ImageBase64;
                if (base64Data.Contains(','))
                {
                    base64Data = base64Data.Split(',')[1];
                }
                var receivedImageBytes = Convert.FromBase64String(base64Data);

                // *** INICIO DE LA NUEVA SOLUCIÓN: SANITIZAR LA IMAGEN ***

                // 2. Cargar los bytes recibidos con ImageSharp y re-codificarlos como un PNG estándar.
                // Esto corrige cualquier problema de formato o corrupción del archivo original.
                byte[] sanitizedPngBytes;
                using (var imageStream = new MemoryStream(receivedImageBytes))
                using (var image = SixLabors.ImageSharp.Image.Load(imageStream))
                using (var sanitizedStream = new MemoryStream())
                {
                    image.Save(sanitizedStream, new PngEncoder());
                    sanitizedPngBytes = sanitizedStream.ToArray();
                }

                // *** FIN DE LA NUEVA SOLUCIÓN ***

                // 3. Crear la imagen para iText usando los bytes sanitizados
                var imageData = iText.IO.Image.ImageDataFactory.Create(sanitizedPngBytes);
                var imageElement = new ITextImage(imageData) // Usamos el alias ITextImage
                    .SetWidth(signature.Width)
                    .SetHeight(signature.Height)
                    .SetFixedPosition(signature.Page, signature.PosX, signature.PosY);

                // 4. Añadir la imagen al PDF
                document.Add(imageElement);
                _log.LogInformation(
                    "Estampada firma (sanitizada) de {Email} en página {Page}",
                    signature.SignerEmail,
                    signature.Page
                );
            }

            document.Close();
            pdfDoc = null;

            intermediateStream.Position = 0;
            await SignDocumentAsync(intermediateStream, outputStream);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error crítico durante el estampado de firmas. Abortando proceso de sellado."
            );
            throw new InvalidOperationException(
                "No se pudo sellar el documento debido a una firma con formato inválido o corrupto.",
                ex
            );
        }
        finally
        {
            pdfDoc?.Close();
        }

        outputStream.Position = 0;
        return outputStream;
    }

    private async Task SignDocumentAsync(Stream inputStream, Stream outputStream)
    {
        // ... (El código de SignDocumentAsync no necesita cambios)
        var pfxPath =
            _config["Certificates:PlatformPfx"]
            ?? throw new InvalidOperationException("PFX no configurado");
        var pfxPassword =
            _config["Certificates:PlatformPassword"]
            ?? throw new InvalidOperationException("Password PFX no configurado");

        if (!File.Exists(pfxPath))
        {
            _log.LogError("El archivo PFX no se encuentra en la ruta: {Path}", pfxPath);
            throw new FileNotFoundException("Certificado PFX no encontrado.", pfxPath);
        }

        var pkcs12 = new Pkcs12Store(
            new FileStream(pfxPath, FileMode.Open, FileAccess.Read),
            pfxPassword.ToCharArray()
        );
        string? alias = pkcs12.Aliases.Cast<string>().FirstOrDefault(a => pkcs12.IsKeyEntry(a));

        if (alias == null)
        {
            _log.LogError("No se encontró una clave privada en el archivo PFX.");
            throw new InvalidOperationException("No se encontró alias de clave privada en PFX.");
        }

        ICipherParameters pk = pkcs12.GetKey(alias).Key;
        X509CertificateEntry[] chain = pkcs12.GetCertificateChain(alias);

        var reader = new PdfReader(inputStream);
        var signer = new PdfSigner(reader, outputStream, new StampingProperties().UseAppendMode());

        signer
            .GetSignatureAppearance()
            .SetReason("Documento sellado por la plataforma")
            .SetLocation("CloudShield Platform");
        signer.SetFieldName("PlatformSignature");

        IExternalSignature pks = new PrivateKeySignature(
            (iText.Commons.Bouncycastle.Crypto.IPrivateKey)pk,
            "SHA-256"
        );
        signer.SignDetached(
            pks,
            (iText.Commons.Bouncycastle.Cert.IX509Certificate[])
                chain.Select(c => c.Certificate).ToArray(),
            null,
            null,
            null,
            0,
            PdfSigner.CryptoStandard.CMS
        );

        _log.LogInformation("Documento sellado digitalmente con éxito usando el certificado.");
    }
}
