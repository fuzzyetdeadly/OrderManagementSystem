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
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
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

        // Assert: that the message was serialized correctly and published to the correct queue
        var expectedRoutingKey = nameof(OrderCreated);
        var jsonBody = Encoding.UTF8.GetString(capturedBody!);
        var receivedMessage = JsonSerializer.Deserialize<OrderCreated>(jsonBody);

        Assert.Equal(expectedRoutingKey, capturedRoutingKey);
        Assert.Equal(message, receivedMessage);
    }
}
