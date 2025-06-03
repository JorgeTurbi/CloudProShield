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
        try
        {
            _log.LogInformation(
                "Procesando CustomerCreatedEvent para cliente {CustomerId} con {FolderCount} carpetas",
                e.CustomerId,
                e.Folders.Count
            );

            await _folders.EnsureStructureAsync(e.CustomerId, e.Folders, ct);

            _log.LogInformation(
                "CustomerCreatedEvent procesado exitosamente → {CustomerId}",
                e.CustomerId
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "Error procesando CustomerCreatedEvent para cliente {CustomerId}",
                e.CustomerId
            );

            // Dependiendo de tu estrategia, podrías:
            // - Re-lanzar la excepción para que RabbitMQ reintente
            // - Enviar a una cola de errores (Dead Letter Queue)
            // - Solo loggear y continuar
            throw; // Para que RabbitMQ sepa que falló
        }
    }
}
