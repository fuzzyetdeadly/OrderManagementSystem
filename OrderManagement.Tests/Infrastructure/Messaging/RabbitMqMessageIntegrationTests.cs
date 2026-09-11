using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderManagement.Application.Messaging;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Tests.Common;
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
        var services = new ServiceCollection();

        services.AddSingleton(mockMediator.Object);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var cancelToken = TestContext.Current.CancellationToken;

        // Note: test containers generate random credentials,
        // so they need to be extracted this way
        var uri = new Uri(_container.GetConnectionString());
        var username = uri.UserInfo.Split(':')[0];
        var password = uri.UserInfo.Split(':')[1];

        await using var publisher = await RabbitMqMessagePublisher<OrderCreated>.CreateAsync(
            hostName: uri.Host, port: uri.Port, username: username, password: password, 
            scopeFactory, cancelToken);

        // Prepare a message
        var message = new OrderCreated(OrderId: 1, CustomerId: 1, CreatedAt: DateTime.UtcNow);

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
}
