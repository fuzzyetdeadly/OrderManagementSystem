using OrderManagement.Application.Models;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;

namespace OrderManagement.Application.Services;

public class OrderService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderRepository _orderRepository;

    public OrderService(ICustomerRepository customerRepository, IOrderRepository orderRepository)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
    }

    /*
     * Returns paginated orders as OrderDtos
     * Results are fully materialized (IEnumerable) before return from  async
     * Caller is responsible for enforcing page/pageSize
     */
    public async Task<IEnumerable<OrderResponse>> GetAllAsync(int page, int pageSize)
    {
        var orders = await _orderRepository.GetAllAsync(page, pageSize);

        return orders.Select(MapToDto);
    }

    public async Task<OrderResponse?> GetOrderIdAsync(int id)
    {
        var order = await _orderRepository.GetOrderIdAsync(id);

        // Null check required in case no order found
        return order is null ? null : MapToDto(order);
    }

    public async Task<OrderResponse?> CreateAsync(CreateOrderRequest dto)
    {
        // Verify that the customer exists
        var customer = await _customerRepository.GetCustomerIdAsync(dto.CustomerId);

        // ToDo: use ErrorOr
        if (customer is null)
            return null;

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
        var createdOrder = await _orderRepository.CreateAsync(order);

        return MapToDto(createdOrder);
    }

    public async Task<OrderResponse?> UpdateStatusAsync(int id, OrderStatus status)
    {
        // Exist check is checked in the repository (not needed here)
        // i.e. Service just needs to forward the parameters directly
        var updatedOrder = await _orderRepository.UpdateStatusAsync(id, status);

        // Null check required in case no order found
        return updatedOrder is null ? null : MapToDto(updatedOrder);
    }

    public async Task<bool> DeleteAsync(int id) => 
        await _orderRepository.DeleteAsync(id);

    private static OrderResponse MapToDto(Order o) => new(
        o.Id, o.Status.ToString(), o.Created, o.CustomerId,
        [.. o.Items.Select(oi =>
            new OrderItemResponse(oi.ProductName, oi.Quantity, oi.UnitPrice)
        )]
    );
}
