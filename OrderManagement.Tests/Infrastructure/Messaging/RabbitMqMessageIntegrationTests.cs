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

/* Note: gaps
Integration test (RabbitMqMessageIntegrationTests.cs)

* Explicit ack verification — test proves the message was consumed/dispatched, not that it was acknowledged in RabbitMQ afterward.
* Concurrent/multiple messages — only a single message is tested; no coverage for ordering or race conditions across multiple messages.
* Null/malformed message deserialization — untested; message gets acked and silently dropped, no verification this is intended.
* Handler exception behavior — if mediator.Publish throws, BasicAckAsync never runs; no test confirms resulting behavior (redelivery, stuck message, etc.) — real production risk (poison-message loop).
* Scope-per-message behavior — the intentional design (_scopeFactory.CreateScope() per message) is untested; nothing confirms a new scope is created each time rather than reused.
* DisposeAsync — no test that channel/connection are actually closed, or that publishing after disposal fails predictably.
* Durable queue survival — reasonable to skip; impractical to test without restarting the broker mid-test.

Priority to close gaps: 

* handler-exception and null-deserialization ack path are the most valuable additions
*/