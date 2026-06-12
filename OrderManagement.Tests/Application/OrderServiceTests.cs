using Moq;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Tests.Common;

namespace OrderManagement.Tests.Application;

public class OrderServiceTests
{
    // Fields
    private readonly Mock<ICustomerRepository> _customerRepo;
    private readonly Mock<IOrderRepository> _orderRepo;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _customerRepo = new Mock<ICustomerRepository>();
        _orderRepo = new Mock<IOrderRepository>();

        // Access '*.Object' for mock instance
        _service = new OrderService(_customerRepo.Object, _orderRepo.Object);
    }

    #region helpers
    private static Order CreateOrder(int id = 1, OrderStatus status = OrderStatus.Pending, int customerId = 1)
    {
        return new Order()
        {
            Id = id,
            CustomerId = customerId,
            Status = status,
            Items = 
            [
                new OrderItem() 
                { 
                    ProductName = "Potato",
                    Quantity = 1,
                    UnitPrice = 0.99m
                }
            ]
        };
    }
    #endregion

    #region GetAll
    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task GetAllAsync_CallsRepoCorrectly()
    {
        // Spoof 'GetAll' to always return exactly one fake order
        // Items returned don't matter. Pagination behavior is owned by the repository
        _orderRepo
            .Setup(r => r.GetAllAsync(It.IsAny<Pagination>()))
            .ReturnsAsync([CreateOrder()]);

        var result = await _service.GetAllAsync(1, 20);

        // Assert that
        // 1. Service correctly maps arguments to pagination record
        // 2. 'r.GetAllAsync' is called once
        _orderRepo.Verify(r => r.GetAllAsync(new Pagination(1, 20)), Times.Once());

        // Sanity check that returns one mapped order (returned by spoof above)
        // This is to cover any mistakes with the service method return mapping logic
        Assert.Single(result);
    }
    #endregion
}
