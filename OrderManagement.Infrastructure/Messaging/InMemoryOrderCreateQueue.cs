using OrderManagement.Application.Messaging;
using System.Threading.Channels;

namespace OrderManagement.Infrastructure.Messaging;

public class InMemoryOrderCreateQueue : IOrderCreatedQueue
{
    private readonly Channel<OrderCreatedMessage> _channel = Channel.CreateUnbounded<OrderCreatedMessage>();

    public async Task PublishAsync(OrderCreatedMessage message, CancellationToken cancelToken = default)
        => await _channel.Writer.WriteAsync(message, cancelToken);

    public IAsyncEnumerable<OrderCreatedMessage> ReadAllAsync(CancellationToken cancelToken = default)
        => _channel.Reader.ReadAllAsync(cancelToken);
}
