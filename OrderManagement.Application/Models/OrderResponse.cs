namespace OrderManagement.Application.Models;

/* Note:
 * Order response only needs to return useful properties to requestor
 * i.e. OrderItem.Id is assumed not to be useful.
 * These types must also be in 'Application' as Service requires them.
 * Their properties are expected to be sane, assuming validation would
 * block bad inputs at controller level, and return null for not found.
 */
public record OrderResponse(
    int Id,
    string Status,
    DateTime Created,
    int CustomerId,
    List<OrderItemResponse> Items
);

public record OrderItemResponse(
    string ProductName,
    int Quantity,
    decimal UnitPrice
);
