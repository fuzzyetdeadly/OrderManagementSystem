namespace OrderManagement.Application.Messaging;

public interface IMessageConsumer<TMessage>
{
    IAsyncEnumerable<TMessage> ReadAllAsync(CancellationToken cancellationToken = default);
}
