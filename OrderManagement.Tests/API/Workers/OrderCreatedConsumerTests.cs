using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using OrderManagement.API.Workers;
using OrderManagement.Application.Messaging;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Tests.Common;
using System.Runtime.CompilerServices;

namespace OrderManagement.Tests.API.Workers;

public class OrderCreatedConsumerTests
{
    private readonly FakeLogger<OrderCreatedConsumer> _logger = new();

    #region helpers
    private OrderCreatedConsumer SetupConsumer(IMessageConsumer<OrderCreated> consumer, IMediator mediator)
    {
        return new(consumer, GetScopeFactory(mediator), _logger);
    }

    private static IServiceScopeFactory GetScopeFactory(IMediator mediator)
    {
        // Note: builds a minimal scope factory that can be used
        // to create a scope with only mediator
        var services = new ServiceCollection();

        services.AddSingleton(mediator);

        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static OrderCreated GetOrderCreatedMessage(int orderId = 1) =>
        new(OrderId: orderId, CustomerId: 1, CreatedAt: DateTime.UtcNow);

    private static async IAsyncEnumerable<OrderCreated> HangingAsyncEnumerable([EnumeratorCancellation] CancellationToken ct)
    {
        // Simulate a hanging queue that can be cancelled via `ct`.
        // The [EnumeratorToken] attribute is required to resolve a parser warning.
        // Without it, "the cancellation token parameter from the generated 'GetAsyncEnumerator'
        // will be unconsumed."
        await Task.Delay(Timeout.Infinite, ct);

        // Unreachable code, but required to satisfy iterator return type
        yield break;
    }

    private static async IAsyncEnumerable<OrderCreated> ThrowingAsyncEnumerable()
    {
        await Task.Yield();

        throw new InvalidOperationException("Simulated queue failure");

#pragma warning disable CS0162 // Unreachable code — required to satisfy iterator return type
        yield break;
#pragma warning restore CS0162
    }
    #endregion

    [Fact]
    [Layer("Api")]
    [Scope("Worker")]
    public async Task ExecuteAsync_DispatchesMessage_ViaMediatR_WhenMessageIsPublished()
    {
        // Arrange: real queue, mock mediator, consumer, cancelToken and message
        var bus = new InMemoryMessageBus<OrderCreated>();
        var mockMediator = new Mock<IMediator>();
        var tcs = new TaskCompletionSource();

        mockMediator
            .Setup(m => m.Publish(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()))
            .Callback(() => tcs.TrySetResult())
            .Returns(Task.CompletedTask);

        var consumer = SetupConsumer(bus, mockMediator.Object);
        var cancelToken = TestContext.Current.CancellationToken;
        var message = GetOrderCreatedMessage(orderId: 1);

        // Act: publish message, then start consumer and wait for dispatch, then stop the consumer
        await bus.PublishAsync(message, cancelToken);
        await consumer.StartAsync(cancelToken);

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2), cancelToken));

        await consumer.StopAsync(cancelToken);

        // Assert: mediator dispatched message exactly once
        Assert.Same(tcs.Task, completedTask);

        mockMediator.Verify(
            m => m.Publish(It.Is<OrderCreated>(e => e.OrderId == 1), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    [Layer("Api")]
    [Scope("Worker")]
    public async Task ExecuteAsync_DispatchesOncePerMessage_WhenMultipleMessagesPublished()
    {
        // Arrange: real queue, consumer and messages
        var bus = new InMemoryMessageBus<OrderCreated>();
        var mockMediator = new Mock<IMediator>();
        var consumer = SetupConsumer(bus, mockMediator.Object);
        var cancelToken = TestContext.Current.CancellationToken;
        var messages = Enumerable.Range(1, 3)
            .Select(i => new OrderCreated(OrderId: i, CustomerId: i, CreatedAt: DateTime.UtcNow))
            .ToList();

        // Act: publish messages, then start, poll, stop consumer
        foreach (var message in messages)
        {
            await bus.PublishAsync(message, cancelToken);
        }

        await consumer.StartAsync(cancelToken);

        // Poll for all 3 dispatches to complete, with deadline timeout
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while(mockMediator.Invocations.Count(i => i.Method.Name == nameof(IMediator.Publish)) < 3 && 
            DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancelToken);
        }

        await consumer.StopAsync(cancelToken);

        // Assert: each message was dispatched exactly once
        foreach (var message in messages)
        {
            mockMediator.Verify(
            m => m.Publish(It.Is<OrderCreated>(e => e.OrderId == message.OrderId), It.IsAny<CancellationToken>()),
            Times.Once());
        }
    }

    [Fact]
    [Layer("Api")]
    [Scope("Worker")]
    public async Task ExecuteAsync_LogsInfoNotError_WhenCancelledDuringShutdown()
    {
        // Arrange: mock queue that hangs until cancelled, consumer and messages
        // Note: for this test, the cancel token of 'ReadAllAsync' is forwarded to 'HangingAsyncEnumerable'
        var mockConsumer = new Mock<IMessageConsumer<OrderCreated>>();

        mockConsumer.Setup(c => c.ReadAllAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => HangingAsyncEnumerable(ct));

        var consumer = SetupConsumer(mockConsumer.Object, Mock.Of<IMediator>());
        var cancelToken = TestContext.Current.CancellationToken;

        // Act: start the consumer and delay it awhile before cancelling with a 'cts'
        // Link 'cts' to the test's own cancellation token, so that if the test
        // runner cancels the test mid-run, 'cts' is cancelled too — ensuring the
        // consumer is always stopped and never left hanging past this test.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);

        await consumer.StartAsync(cts.Token);

        try
        {
            await Task.Delay(50, cancelToken);
        }
        finally
        {
            // StopAsync waits for the consumer's background loop to actually finish
            // and rethrows any exception it hit. This is the real "did it stop cleanly" check.
            await consumer.StopAsync(cts.Token);
        }

        // Assert: info log was written, but no error log was
        // Note: there are two info logs, "started" and "stopped". Only "stopped" is relevant to this test
        var logs = _logger.Collector.GetSnapshot();

        Assert.Contains(logs, log => 
            log.Level == LogLevel.Information && log.Message.Contains("stopped"));

        Assert.DoesNotContain(logs, log => log.Level == LogLevel.Error);
    }

    [Fact]
    [Layer("Api")]
    [Scope("Worker")]
    public async Task ExecuteAsync_StopsPromptly_AfterCancelRequested()
    {
        // Arrange: mock queue that hangs until cancelled, consumer and messages
        var mockConsumer = new Mock<IMessageConsumer<OrderCreated>>();
        mockConsumer.Setup(c => c.ReadAllAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => HangingAsyncEnumerable(ct));

        var consumer = SetupConsumer(mockConsumer.Object, Mock.Of<IMediator>());
        var cancelToken = TestContext.Current.CancellationToken;

        // Act: start the consumer and delay it awhile before cancelling with a 'cts'
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);

        await consumer.StartAsync(cts.Token);

        try
        {
            await Task.Delay(50, cancelToken);
        }
        finally
        {
            // Race StopAsync against a 2-second timeout to confirm it completes promptly.
            var stopTask = consumer.StopAsync(cts.Token);
            var delayTask = Task.Delay(TimeSpan.FromSeconds(2), cancelToken);
            var completedTask = await Task.WhenAny(stopTask, delayTask);

            // Assert: StopAsync completed promptly (within 2 seconds)
            Assert.Same(stopTask, completedTask);

            // Await stopTask to ensure any exception from StopAsync is observed and
            // actually fails the test, rather than being silently discarded.
            await stopTask;
        }
    }

    [Fact]
    [Layer("Api")]
    [Scope("Worker")]
    public async Task ExecuteAsync_LogsError_WhenQueueThrowsUnexpectedException()
    {
        // Arrange: mock queue that throws mid-stream, consumer and messages
        var mockConsumer = new Mock<IMessageConsumer<OrderCreated>>();

        mockConsumer.Setup(c => c.ReadAllAsync(It.IsAny<CancellationToken>()))
            .Returns(ThrowingAsyncEnumerable());

        var consumer = SetupConsumer(mockConsumer.Object, Mock.Of<IMediator>());
        var cancelToken = TestContext.Current.CancellationToken;

        // Act: Start the consumer. BackgroundService.StartAsync only reports failure
        // if ExecuteAsync crashes immediately. Otherwise, it says "started fine" and
        // walks away, even if ExecuteAsync fails moments later. So we poll the logger
        // below to see the error instead of relying on the result of StartAsync.
        await consumer.StartAsync(cancelToken);

        try
        {
            // Poll for any error or the deadline instead of delay
            var deadline = DateTime.UtcNow.AddSeconds(2);

            while (!_logger.Collector.GetSnapshot().Any(log => log.Level == LogLevel.Error) && 
                DateTime.UtcNow < deadline)
            {
                await Task.Delay(20, cancelToken);
            }
        }
        finally
        {
            // StopAsync itself swallows exceptions from the loop (it uses
            // Task.WhenAny internally, which doesn't rethrow). The loop's actual
            // task is exposed separately as consumer.ExecuteTask. That's what
            // rethrows the `throw;` from ExecuteAsync's catch block.
            await consumer.StopAsync(cancelToken);
            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.ExecuteTask!);
        }

        // Assert: error log was written with an exception attached
        var errorLog = _logger.Collector.GetSnapshot()
            .SingleOrDefault(log => log.Level == LogLevel.Error);

        Assert.NotNull(errorLog);
        Assert.NotNull(errorLog.Exception);
        Assert.Contains("unexpected error", errorLog.Message);
    }

    [Fact]
    [Layer("Api")]
    [Scope("Worker")]
    public async Task ExecuteAsync_LogsError_OnHandlerThrow()
    {
        // Arrange: real queue, mock mediator that throws on publish, consumer and message
        var bus = new InMemoryMessageBus<OrderCreated>();
        var mockMediator = new Mock<IMediator>();
        
        mockMediator
            .Setup(m => m.Publish(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated handler failure"));

        var consumer = SetupConsumer(bus, mockMediator.Object);
        var cancelToken = TestContext.Current.CancellationToken;
        var message = GetOrderCreatedMessage(orderId: 1);

        // Act: publish message, then start consumer
        await bus.PublishAsync(message, cancelToken);
        await consumer.StartAsync(cancelToken);

        try
        {
            // Poll until error logged or timeout
            var deadline = DateTime.UtcNow.AddSeconds(2);

            while (!_logger.Collector.GetSnapshot().Any(log => log.Level == LogLevel.Error) && 
                DateTime.UtcNow < deadline)
            {
                await Task.Delay(20, cancelToken);
            }
        }
        finally
        {
            // Shutdown background service and verify that exception is propagated to consumer
            // Note: 'consumer.ExecuteTask' is a reference to the task returned by 'ExecuteAsync'
            await consumer.StopAsync(cancelToken);
            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.ExecuteTask!);
        }

        // Assert: log written with exception attached
        var errorLog = _logger.Collector.GetSnapshot()
            .SingleOrDefault(log => log.Level == LogLevel.Error);

        Assert.NotNull(errorLog);
        Assert.NotNull(errorLog.Exception);
        Assert.Contains("unexpected error", errorLog.Message);
    }
}
