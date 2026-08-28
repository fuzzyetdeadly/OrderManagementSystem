using OrderManagement.Application.Messaging;

namespace OrderManagement.API.Workers;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IOrderCreatedQueue _queue;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(IOrderCreatedQueue queue, ILogger<OrderCreatedConsumer> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("OrderCreatedConsumer started...");

            await foreach (var message in _queue.ReadAllAsync(stoppingToken))
            {
                _logger.LogInformation(
                    "Order {OrderId} created for customer {CustomerId} at {CreatedAt}",
                    message.OrderId, message.CustomerId, message.CreatedAt);
            }
        }
        catch (OperationCanceledException)
        {
            // Expect graceful shutdown, not error
            _logger.LogInformation("OrderCreatedConsumer stopped.");
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OrderCreatedConsumer stopped due to unexpected error.");

            // Re-throw to let host escalate this error
            throw;
        }
    }
}
