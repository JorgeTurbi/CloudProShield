namespace RabbitMQ.Messaging;

public interface IEventBus
{
    void Publish(string routingKey, object message);

    /* antigua ─ la puedes conservar si quieres seguir usándola */
    //void Subscribe<TEvent>(string routingKey, IIntegrationEventHandler<TEvent> handler);

    /* NUEVA ─ con dos genéricos                           */
    void Subscribe<TEvent, THandler>(string routingKey)
        where THandler : IIntegrationEventHandler<TEvent>;
}
