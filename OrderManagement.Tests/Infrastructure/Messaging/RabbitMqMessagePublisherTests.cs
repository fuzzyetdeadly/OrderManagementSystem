using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderManagement.Application.Messaging;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Tests.Common;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderManagement.Tests.Infrastructure.Messaging;

public class RabbitMqMessagePublisherTests
{
    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task PublishAsync_SerializesMessage_AndPublishesToCorrectQueue()
    {
        // Arrange: prepare mock interfaces
        var mockConnection = new Mock<IConnection>();
        var mockChannel = new Mock<IChannel>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();

        // Setup mock channel to capture routing key and body
        string? capturedRoutingKey = null;
        byte[]? capturedBody = null;

        mockChannel
            .Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
                (_, routingKey, _, _, body, _) =>
                {
                    capturedRoutingKey = routingKey;
                    capturedBody = body.ToArray();
                });

        // Instantiate the publisher with mocks
        var publisher = new RabbitMqMessagePublisher<OrderCreated>(
            mockConnection.Object, mockChannel.Object, mockScopeFactory.Object);

        // Prepare a message and cancellation token
        var message = new OrderCreated(OrderId: 1, CustomerId: 1, CreatedAt: DateTime.UtcNow);
        var testCancelToken = TestContext.Current.CancellationToken;

        // Act: post the message
        await publisher.PublishAsync(message, testCancelToken);

        // Assert: that the message was published to the correct queue and serialized correctly
        Assert.NotNull(capturedBody);

        var expectedRoutingKey = nameof(OrderCreated);
        var jsonBody = Encoding.UTF8.GetString(capturedBody);
        var receivedMessage = JsonSerializer.Deserialize<OrderCreated>(jsonBody);

        Assert.Equal(expectedRoutingKey, capturedRoutingKey);
        Assert.Equal(message, receivedMessage);
    }

    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task PublishAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // Note: this is currently a low value test just to verify that the exception
        // is propagated. I decided to keep it for now, in case try/catch is added with throw
        // in future.

        // Arrange: prepare mock interfaces
        var mockConnection = new Mock<IConnection>();
        var mockChannel = new Mock<IChannel>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();

        // Setup mock channel to return a ValueTask from the cancel token
        // This is to simulate RabbitMQ's behavior when a cancellation happens mid-operation
        mockChannel
            .Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
                (_, _, _, _, _, ct) => ValueTask.FromCanceled(ct));

        // Instantiate the publisher with mocks
        var publisher = new RabbitMqMessagePublisher<OrderCreated>(
            mockConnection.Object, mockChannel.Object, mockScopeFactory.Object);

        // Prepare a message and a cancellation token that is already cancelled
        var message = new OrderCreated(OrderId: 1, CustomerId: 1, CreatedAt: DateTime.UtcNow);
        using var cts = new CancellationTokenSource();
        
        cts.Cancel();

        // Act/Assert: expect OperationCanceledException due to cancelled token
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await publisher.PublishAsync(message, cts.Token));
    }
}
