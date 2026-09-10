namespace OrderManagement.Application.Messaging;

public interface IMessagePublisher<TMessage>
{
    Task PublishAsync(TMessage message, CancellationToken cancellationToken = default);
}
