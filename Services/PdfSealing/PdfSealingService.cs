using System.Security.Cryptography.X509Certificates;
using CloudShield.Services.PdfSealing;
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Commons.Bouncycastle.Cert;
using iText.Commons.Bouncycastle.Crypto;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas; //  PdfCanvas
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties; //  TextAlignment, VerticalAlignment
using iText.Signatures;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using RabbitMQ.Contracts.Events;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
    // 1 ───── Escalado de la firma a 300 dpi ─────────────────────────────
    // ───────── util: re-escalado de la firma a 300 dpi ─────────
    private static byte[] UpscaleSignature(string base64, float widthPt, float heightPt)
    {
        const int DPI = 300;
        var clean = base64.Contains(',') ? base64[(base64.IndexOf(',') + 1)..] : base64;
        byte[] raw = Convert.FromBase64String(clean);

        int pxW = (int)(widthPt * DPI / 72f);
        int pxH = (int)(heightPt * DPI / 72f);

        using var src = SixLabors.ImageSharp.Image.Load<Rgba32>(raw);
        using var canvas = new Image<Rgba32>(pxW, pxH, SixLabors.ImageSharp.Color.White);

        src.Mutate(c => c.Resize(pxW, pxH));
        canvas.Mutate(c => c.DrawImage(src, new Point(0, 0), 1f));

        using var ms = new MemoryStream();
        canvas.SaveAsPng(ms);
        return ms.ToArray();
    }

    // ───────── estampado principal ─────────
    private MemoryStream StampImages(byte[] pdfBytes, IReadOnlyList<SignedImageDto> sigs)
    {
        using var src = new MemoryStream(pdfBytes);
        using var dst = new MemoryStream();

        var pdfDoc = new PdfDocument(new PdfReader(src), new PdfWriter(dst));
        var doc = new Document(pdfDoc);

        PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        PdfFont reg = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        const float PAD = 4f,
            LABEL_H = 10f,
            HASH_H = 8f;
        const float R = 6f; // radio
        const float ARM = 10f; // brazos horizontales

        foreach (var s in sigs)
        {
            // ─── 1) Firma (imagen PNG) ──────────────────────────────────────────
            byte[] png = UpscaleSignature(s.ImageBase64, s.Width, s.Height);
            doc.Add(
                new iText.Layout.Element.Image(ImageDataFactory.Create(png))
                    .SetFixedPosition(s.Page, s.PosX, s.PosY)
                    .ScaleToFit(s.Width, s.Height)
            );

            // ═══ geometría general ═════════════════════════════════════════════
            float left = s.PosX;
            float bottom = s.PosY - HASH_H - PAD;
            float right = s.PosX + s.Width;
            float top = s.PosY + s.Height + LABEL_H + PAD;

            float yLabel = top - LABEL_H + 1; // baseline del label
            float yHash = bottom + 2; // baseline del hash

            // ─── 2) Textos ─────────────────────────────────────────────────────
            var cvs = new Canvas(
                new PdfCanvas(pdfDoc.GetPage(s.Page)),
                new iText.Kernel.Geom.Rectangle(left, bottom, right - left, top - bottom)
            );

            cvs.SetFont(bold)
                .SetFontSize(8)
                .ShowTextAligned("Signed by:", left + PAD, yLabel, TextAlignment.LEFT);

            string thumb = s.Thumbprint[..16] + "…";
            cvs.SetFont(reg)
                .SetFontSize(6)
                .SetFontColor(DeviceGray.GRAY)
                .ShowTextAligned(thumb, left + PAD, yHash, TextAlignment.LEFT);

            // ─── 3) Bracket azul  ──────────────────────────────────────────────
            var bc = new PdfCanvas(pdfDoc.GetPage(s.Page))
                .SetStrokeColor(new DeviceRgb(0, 109, 183))
                .SetLineWidth(1.1f)
                .SetLineCapStyle(PdfCanvasConstants.LineCapStyle.ROUND)
                .SetLineJoinStyle(PdfCanvasConstants.LineJoinStyle.ROUND);

            // vertical
            bc.MoveTo(left, bottom + R).LineTo(left, top - R);

            // arco superior 180 ° → 90 °
            bc.Arc(left, top - 2 * R, left + 2 * R, top, 170, -80);

            // brazo superior
            bc.LineTo(left + ARM, top);

            // brazo inferior
            bc.MoveTo(left + ARM, bottom);

            // arco inferior  180 ° → 270 °
            bc.Arc(left, bottom, left + 2 * R, bottom + 2 * R, 180, 80);

            bc.Stroke();

            // ------------- Iniciales ----------------------------------------------
            if (s.InitialStamp is not null)
            {
                var p = s.InitialStamp;

                // contenedor exacto de la cajita
                var area = new iText.Kernel.Geom.Rectangle(p.PosX, p.PosY, p.Width, p.Height);

                var cvsInit = new Canvas(new PdfCanvas(pdfDoc.GetPage(s.Page)), area);

                cvsInit
                    .SetFont(bold)
                    .SetFontSize(9)
                    .ShowTextAligned(
                        p.Text,
                        p.PosX + 2, // un pequeño margen
                        p.PosY + p.Height / 2, // centro vertical
                        TextAlignment.LEFT,
                        VerticalAlignment.MIDDLE,
                        0f
                    ); // 🔑 rotación = 0

                // marco fino
                new PdfCanvas(pdfDoc.GetPage(s.Page))
                    .Rectangle(p.PosX, p.PosY, p.Width, p.Height)
                    .SetLineWidth(0.5f)
                    .Stroke();
            }

            // ------------- Fecha ---------------------------------------------------
            if (s.DateStamp is not null)
            {
                var p = s.DateStamp;

                var area = new iText.Kernel.Geom.Rectangle(p.PosX, p.PosY, p.Width, p.Height);
                var cvsDate = new Canvas(new PdfCanvas(pdfDoc.GetPage(s.Page)), area);

                cvsDate
                    .SetFont(reg)
                    .SetFontSize(8)
                    .ShowTextAligned(
                        p.Text,
                        p.PosX + 2,
                        p.PosY + p.Height / 2,
                        TextAlignment.LEFT,
                        VerticalAlignment.MIDDLE,
                        0f
                    ); // 🔑 rotación = 0
            }
        }

        doc.Close();
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

    private static byte[] CleanPng(string base64)
    {
        // quita el encabezado “data:image/png;base64,” si existe
        var b64 = base64.Contains(',') ? base64[(base64.IndexOf(',') + 1)..] : base64;
        var raw = Convert.FromBase64String(b64);

        using var inMs = new MemoryStream(raw);
        using var image = SharpImage.Load(inMs); // SixLabors.ImageSharp
        using var outMs = new MemoryStream();
        image.Save(outMs, new PngEncoder());
        return outMs.ToArray();
    }
}
