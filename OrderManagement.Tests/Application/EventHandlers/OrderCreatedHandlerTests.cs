using Microsoft.Extensions.Logging;
using Moq;
using OrderManagement.Application.EventHandlers;
using OrderManagement.Application.Messaging;
using OrderManagement.Tests.Common;

namespace OrderManagement.Tests.Application.EventHandlers;

public class OrderCreatedHandlerTests
{
    #region helper
    private static OrderCreated GetOrderCreatedNotification(int orderId = 1, int customerId = 1) =>
        new(OrderId: orderId, CustomerId: customerId, CreatedAt: DateTime.UtcNow);
    #endregion

    [Fact]
    [Layer("Application")]
    [Scope("EventHandler")]
    public async Task Handle_LogsOrderCreatedInfo_WithExpectedFields()
    {
        // Arrange: prepare mock logger, handler and cancelToken
        var mockLogger = new Mock<ILogger<OrderCreatedHandler>>();
        var handler = new OrderCreatedHandler(mockLogger.Object);
        var notification = GetOrderCreatedNotification(orderId: 1, customerId: 2);
        var cancelToken = TestContext.Current.CancellationToken;

        // Arrange: handle the notification
        await handler.Handle(notification, cancelToken);

        // Assert: information called exactly once with expected fields
        // Note: 'LogInformation' is an extension method that isn't on 'ILogging'
        // Thus, verification needs to be done via 'ILogging.Log'
        mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>(
                    (state, _) => state.ToString()!.Contains("Order 1") && 
                        state.ToString()!.Contains("customer 2")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [Fact]
    [Layer("Application")]
    [Scope("EventHandler")]
    public async Task Handle_CompletesSuccessfully()
    {
        // Arrange: create handler, notification and cancel token
        var mockLogger = new Mock<ILogger<OrderCreatedHandler>>();
        var handler = new OrderCreatedHandler(mockLogger.Object);
        var notification = GetOrderCreatedNotification(orderId: 1, customerId: 2);
        var cancelToken = TestContext.Current.CancellationToken;

        // Arrange: handle the notification
        var task = handler.Handle(notification, cancelToken);

        // Assert: task is completed
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    [Layer("Application")]
    [Scope("EventHandler")]
    public async Task Handle_DoesNotThrow_WhenCancelTokenCancelled()
    {
        // Arrange: create handler, notification and cancel token
        var mockLogger = new Mock<ILogger<OrderCreatedHandler>>();
        var handler = new OrderCreatedHandler(mockLogger.Object);
        var notification = GetOrderCreatedNotification(orderId: 1, customerId: 2);
        var cts = new CancellationTokenSource();

        cts.Cancel();

        // Assert: no exception is thrown when the token is cancelled (current behavior)
        // Note: the record class captures exceptions, if any
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(notification, cts.Token));

        Assert.Null(exception);
    }
}
