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

    private OrderCreatedConsumer SetupConsumer(IOrderCreatedQueue queue)
    {
        return new(queue, _logger);
    }

    #region helpers
    private static async IAsyncEnumerable<OrderCreatedMessage> HangingAsyncEnumerable([EnumeratorCancellation] CancellationToken ct)
    {
        // Simulate a hanging queue that can be cancelled via `ct`.
        // The [EnumeratorToken] attribute is required to resolve a parser warning.
        // Without it, "the cancellation token parameter from the generated 'GetAsyncEnumerator'
        // will be unconsumed."
        await Task.Delay(Timeout.Infinite, ct);

        // Unreachable code, but required to satisfy iterator return type
        yield break;
    }

    private static async IAsyncEnumerable<OrderCreatedMessage> ThrowingAsyncEnumerable()
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
    public async Task ExecuteAsync_LogsMessage_WhenMessageIsPublished()
    {
        // Arrange: real queue, consumer, cancelToken and message
        var queue = new InMemoryOrderCreateQueue();
        var consumer = SetupConsumer(queue);
        var cancelToken = TestContext.Current.CancellationToken;
        var message = new OrderCreatedMessage(OrderId: 1, CustomerId: 1, CreatedAt: DateTime.UtcNow);

        // Act: publish message, then start, poll, stop consumer
        await queue.PublishAsync(message, cancelToken);

        var consumerTask = consumer.StartAsync(cancelToken);

        // Poll for expected text instead of fixed delay to avoid flaky tests
        var expectedText = $"Order {message.OrderId} created for customer {message.CustomerId}";
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while(!_logger.Collector.GetSnapshot().Any(log => log.Message.Contains(expectedText)) 
            && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancelToken);
        }

        await consumer.StopAsync(cancelToken);
        await consumerTask;

        // Assert: text is as expected in the logs
        Assert.Contains(_logger.Collector.GetSnapshot(), 
            log => log.Message.Contains(expectedText));
    }

    [Fact]
    [Layer("Api")]
    [Scope("Worker")]
    public async Task ExecuteAsync_LogsOncePerMessage_WhenMultipleMessagesPublished()
    {
        // Arrange: real queue, consumer and messages
        var queue = new InMemoryOrderCreateQueue();
        var consumer = SetupConsumer(queue);
        var cancelToken = TestContext.Current.CancellationToken;
        var messages = Enumerable.Range(1, 3)
            .Select(i => new OrderCreatedMessage(OrderId: i, CustomerId: i, CreatedAt: DateTime.UtcNow))
            .ToList();

        // Act: publish messages, then start, poll, stop consumer
        foreach (var message in messages)
        {
            await queue.PublishAsync(message, cancelToken);
        }

        var consumerTask = consumer.StartAsync(cancelToken);

        // Poll for all expected lines or deadline instead of delay
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while(_logger.Collector.GetSnapshot()
            .Count(log => log.Message.Contains("created for customer")) < 3 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancelToken);
        }

        await consumer.StopAsync(cancelToken);
        await consumerTask;

        // Assert: number of log messages is as expected
        var matchingEntries = _logger.Collector.GetSnapshot()
            .Count(log => log.Level == LogLevel.Information && log.Message.Contains("created for customer"));

        Assert.Equal(3, matchingEntries);
    }

    [Fact]
    [Layer("Api")]
    [Scope("Worker")]
    public async Task ExecuteAsync_LogsInfoNotError_WhenCancelledDuringShutdown()
    {
        // Arrange: mock queue that hangs until cancelled, consumer and messages
        // Note: for this test, the cancel token of 'ReadAllAsync' is forwarded to 'HangingAsyncEnumerable'
        var mockQueue = new Mock<IOrderCreatedQueue>();

        mockQueue.Setup(q => q.ReadAllAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => HangingAsyncEnumerable(ct));

        var consumer = SetupConsumer(mockQueue.Object);
        var cancelToken = TestContext.Current.CancellationToken;

        // Act: start the consumer and delay it awhile before cancelling with a 'cts'
        // Link 'cts' to the test's own cancellation token, so that if the test
        // runner cancels the test mid-run, 'cts' is cancelled too — ensuring the
        // consumer is always stopped and never left hanging past this test.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        var consumerTask = consumer.StartAsync(cts.Token);

        try
        {
            await Task.Delay(50, cancelToken);
        }
        finally
        {
            // Stop the consumer and wait for 'consumerTask' to complete,
            // even if the delay above was cancelled/threw.
            await consumer.StopAsync(cts.Token);
            await consumerTask;
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
    public async Task ExecuteAsync_LogsError_WhenQueueThrowsUnexpectedException()
    {
        // Arrange: mock queue that throws mid-stream, consumer and messages
        var mockQueue = new Mock<IOrderCreatedQueue>();

        mockQueue.Setup(q => q.ReadAllAsync(It.IsAny<CancellationToken>()))
            .Returns(ThrowingAsyncEnumerable());

        var consumer = SetupConsumer(mockQueue.Object);
        var cancelToken = TestContext.Current.CancellationToken;

        // Act: Start the consumer. BackgroundService.StartAsync only reports failure
        // if ExecuteAsync crashes immediately — otherwise it says "started fine" and
        // walks away, even if ExecuteAsync fails moments later. So we don't rely on
        // consumerTask for this; we poll the logger below to see the error instead.
        var consumerTask = consumer.StartAsync(cancelToken);

        // Poll for any error or the deadline instead of delay
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while (!_logger.Collector.GetSnapshot()
            .Any(log => log.Level == LogLevel.Error) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancelToken);
        }

        // Assert: error log was written with an exception attached
        var errorLog = _logger.Collector.GetSnapshot()
            .SingleOrDefault(log => log.Level == LogLevel.Error);

        Assert.NotNull(errorLog);
        Assert.NotNull(errorLog.Exception);
        Assert.Contains("unexpected error", errorLog.Message);
    }
}
