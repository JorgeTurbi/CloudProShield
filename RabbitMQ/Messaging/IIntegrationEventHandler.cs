namespace RabbitMQ.Messaging;

public interface IIntegrationEventHandler<TEvent>
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}
