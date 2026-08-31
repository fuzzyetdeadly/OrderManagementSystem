using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OrderManagement.API.Workers;
using OrderManagement.Application.Messaging;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Tests.Common;

namespace OrderManagement.Tests.API.Workers;

public class OrderCreatedConsumerTests
{
    private readonly FakeLogger<OrderCreatedConsumer> _logger = new();

    private OrderCreatedConsumer SetupConsumer(IOrderCreatedQueue queue)
    {
        return new(queue, _logger);
    }

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
            .Count(entry => entry.Message.Contains("created for customer")) < 3 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancelToken);
        }

        await consumer.StopAsync(cancelToken);
        await consumerTask;

        // Assert: number of log messages is as expected
        var matchingEntries = _logger.Collector.GetSnapshot()
            .Count(entry => entry.Level == LogLevel.Information && entry.Message.Contains("created for customer"));

        Assert.Equal(3, matchingEntries);
    }
}
