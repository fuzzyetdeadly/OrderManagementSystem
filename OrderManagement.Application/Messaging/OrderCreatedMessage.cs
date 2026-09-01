namespace OrderManagement.Application.Messaging;

public record OrderCreatedMessage(int OrderId, int CustomerId, DateTime CreatedAt);
