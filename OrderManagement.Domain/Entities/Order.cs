using System.Text.Json.Serialization;

namespace OrderManagement.Domain.Entities;

public class Order
{
    // Primary key
    public int Id { get; set; }

    // Scalar properties
    // Note:
    // * Setting Created here makes it cross compatible with different DBs
    // * PostgreSQL for backend, SQLite for tests
    // * I chose not to complicate Status with private set and state transition methods.
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int CustomerId { get; set; }
    
    // Navigation
    public Customer Customer { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
}

// This attribute will make swagger generate enum options
// as strings for OrderStatus inputs.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Scheduled
}
