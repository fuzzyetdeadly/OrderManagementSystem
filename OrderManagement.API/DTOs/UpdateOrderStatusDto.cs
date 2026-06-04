using OrderManagement.Domain.Entities;

namespace OrderManagement.API.DTOs;

public record UpdateOrderStatusDto
{
    // Nullable prevents API assigning default of 0 for '{}'
    public OrderStatus? Status { get; init; }
}
