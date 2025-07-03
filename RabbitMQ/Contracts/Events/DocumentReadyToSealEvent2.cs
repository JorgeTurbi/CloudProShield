namespace RabbitMQ.Contracts.Events;

/// <summary>
/// Lanzado por Signature cuando el último firmante termina.
/// NO contiene el PDF ni tokens, sólo metadatos y cada firma en base64.
/// </summary>
public sealed record DocumentReadyToSealEvent2
{
    public Guid Id { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid SignatureRequestId { get; init; }
    public Guid DocumentId { get; init; }
    public IReadOnlyList<SignedImageDto2> Signatures { get; init; } = default!;
};

public sealed record SignedImageDto2
{
    public Guid SignerId { get; init; }
    public string SignerEmail { get; init; } = default!;
    public int Page { get; init; }
    public float PosX { get; init; }
    public float PosY { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public string ImageBase64 { get; init; } = default!;
    public string Thumbprint { get; init; } = default!;
    public DateTime SignedAtUtc { get; init; }
    public string ClientIp { get; init; } = default!;
    public string UserAgent { get; init; } = default!;
    public DateTime ConsentAgreedAtUtc { get; init; }
}
