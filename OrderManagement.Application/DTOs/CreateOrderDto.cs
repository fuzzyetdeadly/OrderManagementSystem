namespace OrderManagement.Application.DTOs;

/*
 * For inbound DTOs, good practice for prefix to indicate purpose
 */
public record CreateOrderDto
{
    public int CustomerId { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = [];
}

public record CreateOrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}