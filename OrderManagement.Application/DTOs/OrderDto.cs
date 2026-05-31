using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.DTOs;

/* Note:
 * Order response only needs to return useful properties to requestor
 * i.e. OrderItem.Id is assumed not to be useful.
 */
public class OrderDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public int CustomerId { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
