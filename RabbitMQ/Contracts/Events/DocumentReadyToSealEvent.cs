namespace RabbitMQ.Contracts.Events;

/// <summary>
/// Emitido por el microservicio Signature cuando **todos** los firmantes
/// han firmado y el PDF puede recibir las firmas visibles + sello LTV.
/// </summary>
public sealed record DocumentReadyToSealEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid SignatureRequestId,
    Guid DocumentId,
    IReadOnlyList<SignedImageDto> Signatures
);

public sealed record InitialStampDto(
    string Text,
    float PosX,
    float PosY,
    float Width,
    float Height
);

public sealed record DateStampDto(string Text, float PosX, float PosY, float Width, float Height);

public sealed record SignedImageDto(
    Guid CustomerId,
    Guid SignerId,
    string SignerEmail,
    int Page,
    float PosX,
    float PosY,
    float Width,
    float Height,
    string ImageBase64, // firma en PNG-base64
    string Thumbprint,
    DateTime SignedAtUtc,
    string ClientIp,
    string UserAgent,
    DateTime ConsentAgreedAtUtc,
    InitialStampDto? InitialStamp,
    DateStampDto? DateStamp
)
{
    public string? Label { get; init; } // “DocuSigned by:”
    public string? HashTxt { get; init; } // “DFD21DC67BF1455…”
};
