using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderManagement.Application.Messaging;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Tests.Common;
using RabbitMQ.Client;
using System.Text;
using Testcontainers.RabbitMq;

namespace OrderManagement.Tests.Infrastructure.Messaging;

[Trait("Category", "Integration")]
public class RabbitMqMessageIntegrationTests : IAsyncLifetime
{
    // Setup RabbitMQ container to use management image for testing
    private readonly RabbitMqContainer _container =
        new RabbitMqBuilder("rabbitmq:4-management").Build();

    public ValueTask InitializeAsync() => new(_container.StartAsync());
    public ValueTask DisposeAsync()
    {
        // Prevent GC from calling finalizer on this object, since we already cleaned up.
        GC.SuppressFinalize(this);

        return _container.DisposeAsync();
    }

    #region helpers

    private static IServiceScopeFactory GetScopeFactory(IMediator mediator)
    {
        // Note: builds a minimal scope factory that can be used
        // to create a scope with only mediator
        var services = new ServiceCollection();

        services.AddSingleton(mediator);

        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private ConnectionFactory GetConnectionFactory()
    {
        // Note: test containers generate random credentials,
        // so they need to be extracted via 'uri.UserInfo'
        var uri = new Uri(_container.GetConnectionString());

        return new()
        {
            HostName = uri.Host,
            Port = uri.Port,
            UserName = uri.UserInfo.Split(':')[0],
            Password = uri.UserInfo.Split(':')[1]
        };
    }

    private static OrderCreated GetOrderCreatedMessage(int orderId = 1) =>
        new(OrderId: orderId, CustomerId: 1, CreatedAt: DateTime.UtcNow);
    #endregion

    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task PublishAsync_MessageIsConsumed_AndDispatchedViaMediatR()
    {
        // Arrange: prepare mock MediatR and task completion source
        var mockMediator = new Mock<IMediator>();
        var tcs = new TaskCompletionSource();

        mockMediator
            .Setup(m => m.Publish(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()))
            .Callback(() => tcs.TrySetResult())
            .Returns(Task.CompletedTask);

        // Setup MediatR and RabbitMQ with service collection
        var cf = GetConnectionFactory();
        var scopeFactory = GetScopeFactory(mockMediator.Object);
        var cancelToken = TestContext.Current.CancellationToken;

        // Note: await using makes publisher auto-dispose at end of method
        // This isn't applicable for integration tests that require manual dispose in their flow.
        await using var publisher = await RabbitMqMessagePublisher<OrderCreated>.CreateAsync(
            cf.HostName, cf.Port, cf.UserName, cf.Password, scopeFactory, cancelToken);

        // Prepare a message
        var message = GetOrderCreatedMessage(orderId: 1);

        // Act: publish the message, and wait for the task completion source to be set
        // by the mock mediator callback (indicating successful dispatch)
        await publisher.PublishAsync(message, cancelToken);

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5), cancelToken));

        // Assert: the completed task is the task completion source
        Assert.Same(tcs.Task, completedTask);

        // And that mediator dispatched the message exactly once
        mockMediator.Verify(
            m => m.Publish(It.Is<OrderCreated>(e => e.OrderId == 1), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task PublishAsync_MessageIsAcknowledged_AndDequeued()
    {
        // Arrange: prepare mock MediatR and task completion source
        var mockMediator = new Mock<IMediator>();
        var tcs = new TaskCompletionSource();

        mockMediator
            .Setup(m => m.Publish(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()))
            .Callback(() => tcs.TrySetResult())
            .Returns(Task.CompletedTask);

        // Create RabbitMQ publisher and message
        var cf = GetConnectionFactory();
        var scopeFactory = GetScopeFactory(mockMediator.Object);
        var cancelToken = TestContext.Current.CancellationToken;
        var publisher = await RabbitMqMessagePublisher<OrderCreated>.CreateAsync(
            cf.HostName, cf.Port, cf.UserName, cf.Password, scopeFactory, cancelToken);

        // Act: publish a message
        var message = GetOrderCreatedMessage(orderId: 2);

        await publisher.PublishAsync(message, cancelToken);

        // Assert: 'tcs' task is completed within 5s
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5), cancelToken));

        Assert.Same(tcs.Task, completedTask);

        // Act: dispose the publisher (closes connection), then reconnect
        // Note: disposing the connection requeues unacked messages
        // If ack succeeded, expect that nothing is requeued
        // Delay is required to ensure ack has time to reach broker
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancelToken);

        await publisher.DisposeAsync();

        // Assert: there are no messages after reconnecting
        await using var connection = await cf.CreateConnectionAsync(cancelToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancelToken);

        // Get messages from the queue
        var result = await channel.BasicGetAsync(queue: nameof(OrderCreated), autoAck: true, cancelToken);

        Assert.Null(result);
    }

    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task PublishAsync_OnHandlerThrow_MessageNotAcknowledged_AndRequeued()
    {
        // Arrange: mock mediator to throw simulated failure
        var mockMediator = new Mock<IMediator>();
        var tcs = new TaskCompletionSource();

        mockMediator
            .Setup(m => m.Publish(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()))
            .Callback(() => tcs.TrySetResult())
            .ThrowsAsync(new InvalidOperationException("Simulated handler failure"));

        // Create RabbitMQ publisher and message
        var cf = GetConnectionFactory();
        var scopeFactory = GetScopeFactory(mockMediator.Object);
        var cancelToken = TestContext.Current.CancellationToken;
        var publisher = await RabbitMqMessagePublisher<OrderCreated>.CreateAsync(
            cf.HostName, cf.Port, cf.UserName, cf.Password, scopeFactory, cancelToken);

        // Act: publish a message
        var message = GetOrderCreatedMessage(orderId: 3);

        await publisher.PublishAsync(message, cancelToken);

        // Assert: 'tcs' task is completed within 5s
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5), cancelToken));

        Assert.Same(tcs.Task, completedTask);

        // Act: let throw unwind before disposing publisher and closing connection
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancelToken);

        await publisher.DisposeAsync();

        // Assert: message is requeued after failure
        await using var connection = await cf.CreateConnectionAsync(cancelToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancelToken);

        // Get messages from the queue
        var result = await channel.BasicGetAsync(queue: nameof(OrderCreated), autoAck: true, cancelToken);

        Assert.NotNull(result);
    }

    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task PublishAsync_WithMalformedMessage_IsNackedAndDropped()
    {
        // Arrange: mock mediator
        var mockMediator = new Mock<IMediator>();
        var cf = GetConnectionFactory();
        var scopeFactory = GetScopeFactory(mockMediator.Object);
        var cancelToken = TestContext.Current.CancellationToken;

        // Create publisher and start consuming
        var publisher = await RabbitMqMessagePublisher<OrderCreated>.CreateAsync(
            cf.HostName, cf.Port, cf.UserName, cf.Password, scopeFactory, cancelToken);

        // Act: publish garbage bytes directly to the queue (bypasses PublishAsync's serialize)
        // to simulate payload that will fail to deserialize in 'consumer.ReceivedAsync'
        await using var connection = await cf.CreateConnectionAsync(cancelToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancelToken);

        var garbageBody = Encoding.UTF8.GetBytes("{ invalid json ]]]");

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: nameof(OrderCreated),
            mandatory: false,
            basicProperties: new BasicProperties(),
            body: garbageBody,
            cancellationToken: cancelToken);

        // Give consumer time to receive and drop the message
        await Task.Delay(TimeSpan.FromSeconds(1), cancelToken);

        await publisher.DisposeAsync();

        // Assert: mediator never invoked for malformed payload
        mockMediator.Verify(m =>
            m.Publish(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()),
            Times.Never());

        // and there are no messages in the queue
        var result = await channel.BasicGetAsync(queue: nameof(OrderCreated), autoAck: true, cancelToken);

        Assert.Null(result);
    }

    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task PublishAsync_AfterDispose_ThrowsBecauseChannelClosed()
    {
        // Arrange: mock mediator
        var mockMediator = new Mock<IMediator>();
        var cf = GetConnectionFactory();
        var scopeFactory = GetScopeFactory(mockMediator.Object);
        var cancelToken = TestContext.Current.CancellationToken;

        // Create publisher and start consuming
        var publisher = await RabbitMqMessagePublisher<OrderCreated>.CreateAsync(
            cf.HostName, cf.Port, cf.UserName, cf.Password, scopeFactory, cancelToken);

        // Dispose the publisher (close connection/channel)
        await publisher.DisposeAsync();

        // Act/Assert: an exception is thrown when publishing message to disposed publisher
        var message = GetOrderCreatedMessage(orderId: 3);

        await Assert.ThrowsAnyAsync<Exception>(
            () => publisher.PublishAsync(message, cancelToken));
    }
}
