namespace RabbitMQ.Contracts.Events;

public sealed record CustomerCreatedEvent
{
    public Guid Id { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CustomerId { get; init; }
    public Guid TaxUserId { get; init; }
    public string FirstName { get; init; } = default!;
    public string? MiddleName { get; init; }
    public string? LastName { get; init; }
    public List<string> Folders { get; init; } = [];
}
