namespace RabbitMQ.Contracts.Events;

public sealed record DocumentAccessRequestedEvent
{
    public Guid Id { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid DocumentId { get; init; }
    public Guid SignerId { get; init; }
    public string SignerEmail { get; init; } = default!;
    public string AccessToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
    public string SessionId { get; init; } = default!;
}
