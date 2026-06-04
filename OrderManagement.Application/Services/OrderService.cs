using ErrorOr;
using OrderManagement.Application.Common;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;

namespace OrderManagement.Application.Services;

// Not using IOrderService was a design choice for this toy project
// It does not intend to cover different service implementations
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
    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(int page, int pageSize)
    {
        var pagination = new Pagination(Page: page, PageSize: pageSize);
        var orders = await _orderRepository.GetAllAsync(pagination);

        return [.. orders.Select(MapToDto)];
    }

    public async Task<IReadOnlyList<OrderResponse>> GetByCustomerIdAsync(int customerId, int page, int pageSize)
    {
        var pagination = new Pagination(Page: page, PageSize: pageSize);
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId, pagination);

        return [.. orders.Select(MapToDto)];
    }

    public async Task<ErrorOr<OrderResponse>> GetOrderIdAsync(int id)
    {
        var order = await _orderRepository.GetOrderIdAsync(id);

        // Prefer Result pattern with ErrorOr instead of returning 'null'
        // 'null' is ambiguous, and the controller shouldn't have to guess it's meaning
        if (order == null)
            return Error.NotFound(code: ErrorCodes.OrderNotFound);
        
        return MapToDto(order);
    }

    public async Task<ErrorOr<OrderResponse>> CreateAsync(CreateOrderRequest dto)
    {
        // Verify that the customer exists
        var exists = await _customerRepository.ExistsAsync(dto.CustomerId);

        if (!exists)
            return Error.NotFound(code: ErrorCodes.CustomerNotFound); ;

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

    public async Task<ErrorOr<OrderResponse>> UpdateStatusAsync(int id, OrderStatus status)
    {
        // Verify that the order exists
        var order = await _orderRepository.GetOrderIdAsync(id);
        if(order is null)
            return Error.NotFound(code: ErrorCodes.OrderNotFound);

        // Design choice to have service own the mutation
        order.Status = status;

        // Return value isn't required as order is a reference to an object from repo.
        await _orderRepository.UpdateAsync(order);

        // Design choice to return order instead of 204
        return MapToDto(order);
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id)
    {
        // Verify that the order exists
        var exists = await _orderRepository.ExistsAsync(id);
        if (!exists)
            return Error.NotFound(code: ErrorCodes.OrderNotFound);

        await _orderRepository.DeleteAsync(id);

        return Result.Deleted;
    }

    private static OrderResponse MapToDto(Order o) => new(
        o.Id, o.Status.ToString(), o.Created, o.CustomerId,
        [.. o.Items.Select(oi =>
            new OrderItemResponse(oi.ProductName, oi.Quantity, oi.UnitPrice)
        )]
    );
}
