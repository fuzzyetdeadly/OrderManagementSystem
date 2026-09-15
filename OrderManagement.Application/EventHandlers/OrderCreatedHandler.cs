using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Messaging;

namespace OrderManagement.Application.EventHandlers;

public class OrderCreatedHandler : INotificationHandler<OrderCreated>
{
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderCreated notification, CancellationToken cancelToken = default)
    {
        _logger.LogInformation(
            "Order {OrderId} created for customer {CustomerId} at {CreatedAt}",
            notification.OrderId, notification.CustomerId, notification.CreatedAt);

        return Task.CompletedTask;
    }
}
