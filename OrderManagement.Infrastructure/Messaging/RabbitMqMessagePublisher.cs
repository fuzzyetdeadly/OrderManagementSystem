using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OrderManagement.Infrastructure.Messaging;

public class RabbitMqMessagePublisher<TMessage> : IMessagePublisher<TMessage>, IAsyncDisposable
    where TMessage : IMessage, INotification
{
    private static readonly string QueueName = typeof(TMessage).Name;

    private readonly IConnection _connection;
    private readonly IChannel _channel;

    // ScopeFactory is used to create scoped MediatR,
    // as MediatR can sometimes depend on scoped services (e.g. DbContext) to handle messages.
    private readonly IServiceScopeFactory _scopeFactory;

    // Note: constructor is internal to allow test project to access it
    internal RabbitMqMessagePublisher(IConnection connection, IChannel channel, IServiceScopeFactory scopeFactory)
    {
        _connection = connection;
        _channel = channel;
        _scopeFactory = scopeFactory;
    }

    public static async Task<RabbitMqMessagePublisher<TMessage>> CreateAsync(
        string hostName, int port, string username, string password, 
        IServiceScopeFactory scopeFactory, CancellationToken cancelToken = default)
    {
        // Use connection factory to create a connection and channel
        var factory = new ConnectionFactory()
        {
            HostName = hostName,
            Port     = port,
            UserName = username,
            Password = password
        };

        var connection = await factory.CreateConnectionAsync(cancelToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancelToken);

        // Declare the queue
        // 'durable: true' allows the queue to survive broker restarts.
        await channel.QueueDeclareAsync(queue: QueueName, 
            durable: true, exclusive: false, autoDelete: false, 
            cancellationToken: cancelToken);

        var instance = new RabbitMqMessagePublisher<TMessage>(connection, channel, scopeFactory);

        // Start receiving messages
        await instance.StartReceivingAsync(cancelToken);

        return instance;
    }

    private async Task StartReceivingAsync(CancellationToken cancelToken = default)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        // When a message is published to RabbitMQ, decode it from raw bytes back into
        // text (JSON), then convert that text into an OrderCreatedMessage object.
        // If it converted successfully, hand it off to the rest of the app to process.
        // Finally, tell RabbitMQ "got it, you can remove this from the queue now."
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var jsonBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var message = JsonSerializer.Deserialize<TMessage>(jsonBody);

                if (message != null)
                {
                    // A scope per message mirrors how MassTransit/NServiceBus handles this.
                    // Handlers needing scoped services (like DbContext) will get a fresh
                    // instance per message, not one shared for the process lifetimem
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    await mediator.Publish(message, cancelToken);
                }

                // Acknowledge only after the handler successfully processes the message
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancelToken);
            }
            catch(JsonException)
            {
                // Nack messages that can't be deserialized, no requeue
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancelToken);
            }
        };

        // Start consuming messages from the queue
        await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer, cancelToken);
    }

    public async Task PublishAsync(TMessage message, CancellationToken cancelToken = default)
    {
        var jsonBody = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(jsonBody);

        // Note: 'basicProperties' is configured to use default properties.
        // These are metadata about the message, such as content type, delivery mode, etc.
        // 'mandatory' is set to false. Messages that can't be routed to a queue will be dropped.
        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            mandatory: false,
            basicProperties: new BasicProperties(),
            body: body,
            cancellationToken: cancelToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();

        // Prevent GC from calling finalizer on this object, since we already cleaned up.
        GC.SuppressFinalize(this);
    }
}
