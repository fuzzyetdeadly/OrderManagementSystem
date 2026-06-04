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
    public int? CustomerId { get; init; }
    public List<CreateOrderItemDto> Items { get; init; } = [];
}

public record CreateOrderItemDto
{
    public string? ProductName { get; init; }
    public int? Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
}