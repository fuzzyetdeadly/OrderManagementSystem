using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderManagement.Application.Messaging;
using OrderManagement.Tests.Common;

namespace OrderManagement.Tests.Application.Messaging;

public class EventBusTests
{
    [Fact]
    [Layer("Application")]
    [Scope("Messaging")]
    public async Task PublishAsync_ResolvesAndCallsCorrectPublisher()
    {
        // Arrange: create and register mock publisher for OrderCreated event
        var mockPublisher = new Mock<IMessagePublisher<OrderCreated>>();

        var services = new ServiceCollection();

        services.AddSingleton(mockPublisher.Object);

        // Register EventBus as singleton
        var provider = services.BuildServiceProvider();
        var eventBus = new EventBus(provider);

        // Prepare event and cancel token for act
        var @event = new OrderCreated(OrderId: 1, CustomerId: 1, CreatedAt: DateTime.UtcNow);
        var cancelToken = TestContext.Current.CancellationToken;

        // Act: publish the event
        await eventBus.PublishAsync(@event, cancelToken);

        // Assert: verify that mock publisher is called with correct event and token
        mockPublisher.Verify(p => p.PublishAsync(
            It.Is<OrderCreated>(e => e.OrderId == 1), cancelToken),
            Times.Once());
    }

    [Fact]
    [Layer("Application")]
    [Scope("Messaging")]
    public async Task PublishAsync_ThrowsInvalidOperation_WhenPublisherNotRegistered()
    {
        // Arrange: create EventBus without registering any publisher
        var provider = new ServiceCollection().BuildServiceProvider();
        var eventBus = new EventBus(provider);

        // Prepare event for act
        var @event = new OrderCreated(OrderId: 1, CustomerId: 1, CreatedAt: DateTime.UtcNow);

        // Act/Assert: expect InvalidOperationException due to missing publisher registration
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await eventBus.PublishAsync(@event, TestContext.Current.CancellationToken));
    }
}