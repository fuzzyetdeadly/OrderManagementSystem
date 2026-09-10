using OrderManagement.Application.Messaging;
using System.Threading.Channels;

namespace OrderManagement.Infrastructure.Messaging;

// Generic in-memory message bus implementation to pub/sub concrete TMessage types
public class InMemoryMessageBus<TMessage> : IMessagePublisher<TMessage>, IMessageConsumer<TMessage>
    where TMessage: IMessage
{
    private readonly Channel<TMessage> _channel = Channel.CreateUnbounded<TMessage>();

    public async Task PublishAsync(TMessage message, CancellationToken cancelToken = default)
        => await _channel.Writer.WriteAsync(message, cancelToken);

    public IAsyncEnumerable<TMessage> ReadAllAsync(CancellationToken cancelToken = default)
        => _channel.Reader.ReadAllAsync(cancelToken);
}
