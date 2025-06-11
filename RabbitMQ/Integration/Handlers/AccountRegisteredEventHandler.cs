using RabbitMQ.Contracts.Events;
using RabbitMQ.Messaging;
using Services.UserServices;

namespace RabbitMQ.Integration.Handlers;

public sealed class AccountRegisteredEventHandler : IIntegrationEventHandler<AccountRegisteredEvent>
{
    private readonly IUserCreateByTaxPro _prov;
    private readonly ILogger<AccountRegisteredEventHandler> _log;

    public AccountRegisteredEventHandler(
        IUserCreateByTaxPro prov,
        ILogger<AccountRegisteredEventHandler> log
    )
    {
        _prov = prov;
        _log = log;
    }

    public async Task HandleAsync(AccountRegisteredEvent e, CancellationToken ct)
    {
        try
        {
            await _prov.ProvisionAsync(e, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error aprovisionando {Email}", e.Email);
            throw;
        }
    }
}
