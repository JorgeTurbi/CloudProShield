namespace RabbitMQ.Messaging.Factory;

public interface IEventBusFactory
{
  IEventBus CreateEventBus();
}