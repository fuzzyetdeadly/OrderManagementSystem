namespace OrderManagement.Application.Messaging;

public interface IMessageConsumer<TMessage> where TMessage : IMessage
{
    IAsyncEnumerable<TMessage> ReadAllAsync(CancellationToken cancellationToken = default);
}
