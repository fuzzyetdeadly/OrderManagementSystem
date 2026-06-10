using OrderManagement.Domain.Interfaces;

namespace OrderManagement.Domain.Entities;

// Note: 'required' indicates ProductName must be set on construct
public class OrderItem : IEntity
{
    public int Id { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
}
