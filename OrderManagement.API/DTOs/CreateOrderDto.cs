using OrderManagement.API.Constants;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.API.DTOs;

/*
 * For inbound DTOs, good practice for prefix to indicate purpose
 * Properties are marked with ? to ensure they are null when unprovided
 * 'init' allows only set on object creation
 */
public record CreateOrderDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = ErrorMessages.Order.InvalidCustomerId)]
    public int? CustomerId { get; init; }
    public List<CreateOrderItemDto> Items { get; init; } = [];
}

public record CreateOrderItemDto
{
    [Required]
    [MinLength(1, ErrorMessage = ErrorMessages.OrderItem.InvalidProductNameLength)]
    [MaxLength(50, ErrorMessage = ErrorMessages.OrderItem.InvalidProductNameLength)]
    public string ProductName { get; init; } = string.Empty;
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = ErrorMessages.OrderItem.InvalidQuantity)]
    public int Quantity { get; init; }
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = ErrorMessages.OrderItem.InvalidUnitPrice)]
    public decimal UnitPrice { get; init; }
}