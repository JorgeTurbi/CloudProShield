using RabbitMQ.Contracts.Events;

namespace Services.UserServices;

public interface IUserCreateByTaxPro
{
    Task ProvisionAsync(AccountRegisteredEvent evt, CancellationToken ct = default);
}
