namespace OrderManagement.Domain.Entities;

public class Order
{
    // Primary key
    public int Id { get; set; }

    // Scalar properties
    public OrderStatus Status { get; set; }
    public DateTime Created { get; set; }
    
    // Foreign keys
    public int CustomerId { get; set; }
    
    // Navigation
    public Customer Customer { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Scheduled
}
