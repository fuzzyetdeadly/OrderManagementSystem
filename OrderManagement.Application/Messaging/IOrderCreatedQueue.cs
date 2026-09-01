namespace OrderManagement.Application.Messaging;

public interface IOrderCreatedQueue
{
    Task PublishAsync(OrderCreatedMessage message, CancellationToken cancelToken = default);
    IAsyncEnumerable<OrderCreatedMessage> ReadAllAsync(CancellationToken cancelToken = default);
}
