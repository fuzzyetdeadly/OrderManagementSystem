using OrderManagement.Application.Messaging;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Tests.Common;

namespace OrderManagement.Tests.Infrastructure.Messaging;

public class InMemoryOrderCreateQueueTests
{
    // No mocking needed, test the actual queue implementation
    private readonly InMemoryOrderCreateQueue _queue = new();

    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task PublishAsync_MessageIsReadable_ViaReadAllAsync()
    {
        // Arrange
        var message = new OrderCreatedMessage(OrderId: 1, CustomerId: 1, CreatedAt: DateTime.UtcNow);

        // Act
        var cancelToken = TestContext.Current.CancellationToken;

        await _queue.PublishAsync(message, cancelToken);
        
        await using var messageEnumerator = _queue
            .ReadAllAsync(cancelToken)
            .GetAsyncEnumerator(cancelToken);

        // Assert
        Assert.True(await messageEnumerator.MoveNextAsync());
        Assert.Equal(message, messageEnumerator.Current);
    }

    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task PublishAsync_MultipleMessages_ReadInFifoOrder()
    {
        // Arrange
        var messages = new List<OrderCreatedMessage>
        {
            new(OrderId: 1, CustomerId: 1, CreatedAt: DateTime.UtcNow),
            new(OrderId: 2, CustomerId: 2, CreatedAt: DateTime.UtcNow.AddSeconds(1)),
            new(OrderId: 3, CustomerId: 3, CreatedAt: DateTime.UtcNow.AddSeconds(2))
        };

        // Act
        var cancelToken = TestContext.Current.CancellationToken;

        foreach (var message in messages)
        {
            await _queue.PublishAsync(message, cancelToken);
        }

        await using var messageEnumerator = _queue
            .ReadAllAsync(cancelToken)
            .GetAsyncEnumerator(cancelToken);

        // Assert
        foreach (var expectedMessage in messages)
        {
            Assert.True(await messageEnumerator.MoveNextAsync());
            Assert.Equal(expectedMessage, messageEnumerator.Current);
        }
    }

    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task PublishAsync_CancelledToken_ThrowsOperationCanceled()
    {
        // Arrange
        var message = new OrderCreatedMessage(OrderId: 1, CustomerId: 1, CreatedAt: DateTime.UtcNow);
        using var cts = new CancellationTokenSource();

        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _queue.PublishAsync(message, cts.Token));
    }

    [Fact]
    [Layer("Infrastructure")]
    [Scope("Messaging")]
    public async Task ReadAllAsync_CancelledToken_ThrowsOperationCanceled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _queue.ReadAllAsync(cts.Token))
            {
                // This block should not be executed
            }
        });
    }
}
