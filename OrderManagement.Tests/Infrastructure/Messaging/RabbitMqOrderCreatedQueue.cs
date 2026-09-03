using OrderManagement.Application.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace OrderManagement.Tests.Infrastructure.Messaging;

public class RabbitMqOrderCreatedQueue : IOrderCreatedQueue, IAsyncDisposable
{
    private const string QueueName = "order-created";

    private readonly IConnection _connection;
    private readonly IChannel _channel;

    // RabbitMQ delivers messages to this channel, and the rest of the app will read from it.
    // Also decouples RabbitMQ from app, and converts it's event-driven push model into
    // something the app can consume as a plain async enumerable.
    // i.e. It's an abstraction/isolation layer that keeps RabbitMQ logic contained in this class
    private readonly Channel<OrderCreatedMessage> _localBridge = Channel.CreateUnbounded<OrderCreatedMessage>();

    private RabbitMqOrderCreatedQueue(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<RabbitMqOrderCreatedQueue> CreateAsync(
        string hostName, string username, string password, CancellationToken cancelToken = default)
    {
        // Use connection factory to create a connection and channel
        var factory = new ConnectionFactory()
        {
            HostName = hostName,
            UserName = username,
            Password = password
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Declare the queue
        await channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false);

        var instance = new RabbitMqOrderCreatedQueue(connection, channel);

        // Start consuming messages
        await instance.StartConsumingAsync(cancelToken);

        return instance;
    }

    private async Task StartConsumingAsync(CancellationToken cancelToken = default)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        // When a message arrives from RabbitMQ, decode it from raw bytes back into
        // text (JSON), then convert that text into an OrderCreatedMessage object.
        // If it converted successfully, hand it off to the rest of the app to process.
        // Finally, tell RabbitMQ "got it, you can remove this from the queue now."
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var jsonBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var message = JsonSerializer.Deserialize<OrderCreatedMessage>(jsonBody);

            if (message != null)
            {
                // Publish the message to the local bridge channel for processing by the rest of the app
                await _localBridge.Writer.WriteAsync(message, cancelToken);
            }

            // Acknowledge the message to RabbitMQ so it can be removed from the queue
            await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancelToken);
        };

        // Start consuming messages from the queue
        await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer, cancelToken);
    }

    public async Task PublishAsync(OrderCreatedMessage message, CancellationToken cancelToken = default)
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

    public IAsyncEnumerable<OrderCreatedMessage> ReadAllAsync(CancellationToken cancelToken = default)
        => _localBridge.Reader.ReadAllAsync(cancelToken);

    public async ValueTask DisposeAsync()
    {
        _localBridge.Writer.TryComplete();

        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
