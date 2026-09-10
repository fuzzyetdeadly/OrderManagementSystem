namespace OrderManagement.Application.Messaging;

public interface IMessagePublisher<TMessage> where TMessage: IMessage
{
    Task PublishAsync(TMessage message, CancellationToken cancellationToken = default);
}
