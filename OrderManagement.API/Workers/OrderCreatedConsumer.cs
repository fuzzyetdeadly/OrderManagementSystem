using MediatR;
using OrderManagement.Application.Messaging;

namespace OrderManagement.API.Workers;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IMessageConsumer<OrderCreated> _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(IMessageConsumer<OrderCreated> bus, IServiceScopeFactory scopeFactory, ILogger<OrderCreatedConsumer> logger)
    {
        _bus = bus;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("OrderCreatedConsumer started...");

            await foreach (var message in _bus.ReadAllAsync(stoppingToken))
            {
                // Dispatch the message as a notification using mediator
                // While handler has no scoped dependencies, IMediator can be DI directly.
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await mediator.Publish(message, stoppingToken);
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
