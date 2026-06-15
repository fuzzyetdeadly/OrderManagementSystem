using Moq;
using OrderManagement.Application.Common;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Tests.Common;

namespace OrderManagement.Tests.Application;

public class OrderServiceTests
{
    // Constants
    protected readonly static Pagination _pagination = new(Page: 1, PageSize: 20);

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

    private void SetupCustomerExists(bool exists = true)
    {
        _customerRepo
            .Setup(r => r.ExistsAsync(It.IsAny<int>()))
            .ReturnsAsync(exists);
    }

    private void SetupGetByCustomerId(params Order[] orders)
    {
        // Returns one order by default
        _orderRepo
            .Setup(r => r.GetByCustomerIdAsync(It.IsAny<int>(), It.IsAny<Pagination>()))
            .ReturnsAsync(orders.Length > 0 ? orders : [CreateOrder()]);
    }
    #endregion

    #region GetAll
    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task GetAll_CallsRepoCorrectly()
    {
        // Spoof 'GetAll' to always return exactly one fake order
        // Items returned don't matter. Pagination behavior is owned by the repository
        _orderRepo
            .Setup(r => r.GetAllAsync(It.IsAny<Pagination>()))
            .ReturnsAsync([CreateOrder()]);

        // Note: for tests, prefer magic numbers over constants
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

    #region GetByCustomerId
    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task GetByCustomerId_ReturnsNotFound_WhenCustomerNotFound()
    {
        SetupCustomerExists(false);

        int customerId = 1;
        var result = await _service.GetByCustomerIdAsync(customerId, 1, 20);

        // Verify that exists method was invoked, but get method is not
        _customerRepo.Verify(r => r.ExistsAsync(customerId), Times.Once());
        _orderRepo.Verify(r => r.GetByCustomerIdAsync(It.IsAny<int>(), It.IsAny<Pagination>()), Times.Never());

        Assert.True(result.IsError);
        Assert.Equal(ErrorCodes.CustomerNotFound, result.FirstError.Code);
    }

    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task GetByCustomerId_ReturnsOrders_WhenCustomerFound()
    {
        SetupCustomerExists();
        SetupGetByCustomerId();

        int customerId = 1;
        var result = await _service.GetByCustomerIdAsync(customerId, 1, 20);

        // Verify that exists method and get methods are both invoked
        _customerRepo.Verify(r => r.ExistsAsync(customerId), Times.Once());
        _orderRepo.Verify(r => r.GetByCustomerIdAsync(customerId, new Pagination(1, 20)), Times.Once());

        Assert.False(result.IsError);

        // Sanity check one item was returned
        Assert.Single(result.Value);
    }
    #endregion
}
