namespace OrderManagement.Application.Models;

// Concise record declaration better for requests
// Expect that DTOs should have validated them correctly beforehand
public record CreateOrderRequest(
    int CustomerId, 
    List<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    string ProductName, 
    int Quantity, 
    decimal UnitPrice
);