using System.Security.Cryptography.X509Certificates;
using CloudShield.Services.PdfSealing;
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Commons.Bouncycastle.Cert;
using iText.Commons.Bouncycastle.Crypto;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Signatures;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using RabbitMQ.Contracts.Events;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using ITextImage = iText.Layout.Element.Image;
using SharpImage = SixLabors.ImageSharp.Image;

public sealed class PdfSealingService : IPdfSealingService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<PdfSealingService> _log;

    public PdfSealingService(IConfiguration cfg, ILogger<PdfSealingService> log) =>
        (_cfg, _log) = (cfg, log);

    public async Task<MemoryStream> ApplySignaturesAndSealAsync(
        Stream originalPdf,
        IReadOnlyList<SignedImageDto> signatures
    )
    {
        using var workMs = new MemoryStream();
        await originalPdf.CopyToAsync(workMs);
        workMs.Position = 0;

        using var stampedMs = StampImages(workMs.ToArray(), signatures);

        // ⬇️ convertimos MemoryStream → byte[]
        byte[] sealedBytes = SignDocument(stampedMs.ToArray());

        var output = new MemoryStream(sealedBytes);
        output.Position = 0;
        return output;
    }

    // ---------- Estampar firmas visibles ---------------------------------
    private MemoryStream StampImages(byte[] pdfBytes, IReadOnlyList<SignedImageDto> signatures)
    {
        using var src = new MemoryStream(pdfBytes);
        using var dst = new MemoryStream();

        var pdfDoc = new PdfDocument(new PdfReader(src), new PdfWriter(dst));
        var document = new Document(pdfDoc);

        foreach (var sig in signatures)
        {
            var b64 = sig.ImageBase64.Contains(',')
                ? sig.ImageBase64.Split(',')[1]
                : sig.ImageBase64;
            var raw = Convert.FromBase64String(b64);

            byte[] cleanPng;
            using (var imgIn = new MemoryStream(raw))
            using (var img = SharpImage.Load(imgIn))
            using (var imgOut = new MemoryStream())
            {
                img.Save(imgOut, new PngEncoder());
                cleanPng = imgOut.ToArray();
            }

            var itImg = new ITextImage(ImageDataFactory.Create(cleanPng))
                .SetWidth(sig.Width)
                .SetHeight(sig.Height)
                .SetFixedPosition(sig.Page, sig.PosX, sig.PosY);

            document.Add(itImg);
            _log.LogInformation(
                "Estampada firma de {Mail} en pág {Page}",
                sig.SignerEmail,
                sig.Page
            );
        }

        document.Close(); // cierra pdfDoc & writer
        return dst;
    }

    // ---------- Sello digital --------------------------------------------
    private byte[] SignDocument(byte[] stampedPdf)
    {
        var pfxPath = _cfg["Certificates:PlatformPfx"] ?? throw new("PlatformPfx no set");
        var pfxPass = _cfg["Certificates:PlatformPassword"] ?? throw new("PlatformPassword no set");

        using var fs = File.OpenRead(pfxPath);
        var store = new Pkcs12Store(fs, pfxPass.ToCharArray());

        string alias = store.Aliases.Cast<string>().First(store.IsKeyEntry);
        AsymmetricKeyParameter bcKey = store.GetKey(alias).Key;
        var ceChain = store.GetCertificateChain(alias);

        IPrivateKey privKey = new PrivateKeyBC(bcKey);
        IX509Certificate[] chain = ceChain
            .Select(e => (IX509Certificate)new X509CertificateBC(e.Certificate))
            .ToArray();

        using var inMs = new MemoryStream(stampedPdf);
        using var outMs = new MemoryStream();

        // ⬇️ PdfSigner NO implementa IDisposable → sin 'using'
        var signer = new PdfSigner(
            new PdfReader(inMs),
            outMs,
            new StampingProperties().UseAppendMode()
        );

        signer
            .GetSignatureAppearance()
            .SetReason("Documento sellado por la plataforma")
            .SetLocation("CloudShield Platform");

        signer.SetFieldName("PlatformSignature");

        var pks = new PrivateKeySignature(privKey, DigestAlgorithms.SHA256);
        signer.SignDetached(pks, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);

        signer.GetDocument().Close(); // asegura flush

        _log.LogInformation("Documento sellado digitalmente de forma exitosa.");
        return outMs.ToArray();
    }
}
