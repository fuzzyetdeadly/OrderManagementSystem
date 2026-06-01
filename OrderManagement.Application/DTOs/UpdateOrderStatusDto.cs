using OrderManagement.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.DTOs;

public record UpdateOrderStatusDto
{
    // Nullable prevents API assigning default of 0 for '{}'
    // Required can then catch this and return 400 correctly
    [Required] public OrderStatus? Status { get; set; }
}
