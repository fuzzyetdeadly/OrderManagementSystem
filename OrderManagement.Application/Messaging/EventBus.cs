using Microsoft.Extensions.DependencyInjection;

namespace OrderManagement.Application.Messaging;

public class EventBus : IEventBus
{
    private readonly IServiceProvider _provider;

    public EventBus(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Event bus is a dispatcher that takes an event and publishes it to the appropriate message publisher
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="event"></param>
    /// <param name="cancelToken"></param>
    /// <returns>Task</returns>
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancelToken = default) where TEvent : IEvent
    {
        var publisher = _provider.GetRequiredService<IMessagePublisher<TEvent>>();

        return publisher.PublishAsync(@event, cancelToken);
    }
}
