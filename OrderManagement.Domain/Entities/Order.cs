using System.Text.Json.Serialization;

namespace OrderManagement.Domain.Entities;

public class Order
{
    // Primary key
    public int Id { get; set; }

    // Scalar properties
    // Note: DateTime is configured to be set by EF Core at DB level
    public OrderStatus Status { get; set; }
    public DateTime Created { get; set; }
    
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
