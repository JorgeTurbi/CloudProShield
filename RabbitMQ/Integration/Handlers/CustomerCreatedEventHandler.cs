using CloudShield.Services.OperationStorage;
using RabbitMQ.Contracts.Events;
using RabbitMQ.Messaging;

namespace RabbitMQ.Integration.Handlers;

public sealed class CustomerCreatedEventHandler : IIntegrationEventHandler<CustomerCreatedEvent>
{
    private readonly IFolderProvisioner _folders;
    private readonly ILogger<CustomerCreatedEventHandler> _log;

    public CustomerCreatedEventHandler(
        IFolderProvisioner folders,
        ILogger<CustomerCreatedEventHandler> log
    )
    {
        _folders = folders;
        _log = log;
    }

    public async Task HandleAsync(CustomerCreatedEvent e, CancellationToken ct)
    {
        await _folders.EnsureStructureAsync(e.CustomerId, e.Folders, ct);
        _log.LogInformation("CustomerCreatedEvent procesado → {Cust}", e.CustomerId);
    }
}
