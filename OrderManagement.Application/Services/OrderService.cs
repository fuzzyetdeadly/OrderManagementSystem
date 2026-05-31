using OrderManagement.Application.DTOs;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;

namespace OrderManagement.Application.Services;

public class OrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    /*
     * Returns paginated orders as OrderDtos
     * Results are fully materialized (IEnumerable) before return from  async
     * Caller is responsible for enforcing page/pageSize
     */
    public async Task<IEnumerable<OrderDto>> GetAllAsync(int page, int pageSize)
    {
        var orders = await _repository.GetAllAsync(page, pageSize);

        return orders.Select(MapToDto);
    }

    public async Task<OrderDto?> GetOrderIdAsync(int id)
    {
        var order = await _repository.GetOrderIdAsync(id);

        // Null check required in case no order found
        return order is null ? null : MapToDto(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
    {
        // Map the order and pass it to repository
        var order = new Order()
        {
            CustomerId = dto.CustomerId,
            Items = [.. dto.Items.Select(i => new OrderItem()
            {
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            })]
        };

        // Note: expect order create process to return
        // an order updated with assigned Id and Create time
        var createdOrder = await _repository.CreateAsync(order);

        return MapToDto(createdOrder);
    }

    public async Task<OrderDto?> UpdateStatusAsync(int id, OrderStatus status)
    {
        // Exist check is checked in the repository (not needed here)
        // i.e. Service just needs to forward the parameters directly
        var updatedOrder = await _repository.UpdateStatusAsync(id, status);

        // Null check required in case no order found
        return updatedOrder is null ? null : MapToDto(updatedOrder);
    }

    public async Task<bool> DeleteAsync(int id) => 
        await _repository.DeleteAsync(id);

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        Status = o.Status.ToString(),
        Created = o.Created,
        CustomerId = o.CustomerId,
        Items = [.. o.Items.Select(oi => new OrderItem()
        {
            ProductName = oi.ProductName,
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
        })]
    };
}
