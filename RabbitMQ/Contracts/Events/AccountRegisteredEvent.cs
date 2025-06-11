namespace RabbitMQ.Contracts.Events;

public sealed record AccountRegisteredEvent
{
    public Guid Id { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public bool IsCompany { get; init; }
    public Guid? CompanyId { get; init; }
    public string? FullName { get; set; }
    public string? CompanyName { get; init; }
    public string? Domain { get; init; }
}
