using Microsoft.Extensions.Logging.Testing;
using OrderManagement.API.Workers;
using OrderManagement.Application.Messaging;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Tests.Common;

namespace OrderManagement.Tests.API.Workers;

public class OrderCreatedConsumerTests
{
    private readonly InMemoryOrderCreateQueue _queue = new();
    private readonly FakeLogger<OrderCreatedConsumer> _logger = new();
    private readonly OrderCreatedConsumer _consumer;

    public OrderCreatedConsumerTests()
    {
        _consumer = new OrderCreatedConsumer(_queue, _logger);
    }

    [Fact]
    [Layer("Api")]
    [Scope("Worker")]
    public async Task ExecuteAsync_LogsMessage_WhenMessageIsPublished()
    {
        // Arrange
        var message = new OrderCreatedMessage(OrderId: 1, CustomerId: 1, CreatedAt: DateTime.UtcNow);

        // Act
        var cancelToken = TestContext.Current.CancellationToken;

        // Start the consumer in the background
        var consumerTask = _consumer.StartAsync(cancelToken);

        await _queue.PublishAsync(message, cancelToken);

        // Poll for expected textinstead of fixed delay to avoid flaky tests
        var expectedText = $"Order {message.OrderId} created for customer {message.CustomerId}";
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while(!_logger.Collector.GetSnapshot().Any(log => log.Message.Contains(expectedText)) 
            && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancelToken);
        }

        // Stop the consumer
        await _consumer.StopAsync(cancelToken);
        await consumerTask;

        // Assert: text is as expected in the logs
        Assert.Contains(_logger.Collector.GetSnapshot(), 
            log => log.Message.Contains(expectedText));
    }
}
