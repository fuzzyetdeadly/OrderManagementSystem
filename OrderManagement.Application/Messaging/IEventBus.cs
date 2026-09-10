namespace OrderManagement.Application.Messaging;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancelToken = default) where TEvent : IEvent;
}
