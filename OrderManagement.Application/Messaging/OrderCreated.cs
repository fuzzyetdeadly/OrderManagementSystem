namespace OrderManagement.Application.Messaging;

public record OrderCreated(int OrderId, int CustomerId, DateTime CreatedAt) : IEvent;
