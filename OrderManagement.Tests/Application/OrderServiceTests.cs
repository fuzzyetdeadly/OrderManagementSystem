using ErrorOr;
using Moq;
using OrderManagement.Application.Common;
using OrderManagement.Application.Models;
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

    private static CreateOrderRequest CreatePostRequest()
    {
        return new(1, [new CreateOrderItemRequest("Potato", 1, 0.99m)]);
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

    private void SetupGetById(Order order = null!)
    {
        // Returns order specified by argument
        // If none is supplied uses a default order
        // If 'null!' is supplied, returns null
        _orderRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(order);
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
    public async Task GetByCustomerId_CallsRepoCorrectly_WhenCustomerFound()
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

    #region GetById
    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task GetById_ReturnsNotFound_WhenOrderNotFound()
    {
        SetupGetById();

        int orderId = 1;
        var result = await _service.GetByIdAsync(orderId);

        // Verify that expected method is called
        _orderRepo.Verify(r => r.GetByIdAsync(orderId), Times.Once());

        Assert.True(result.IsError);
        Assert.Equal(ErrorCodes.OrderNotFound, result.FirstError.Code);
    }

    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task GetById_ReturnsOrder_WhenOrderFound()
    {
        SetupGetById(CreateOrder());

        int orderId = 1;
        var result = await _service.GetByIdAsync(orderId);

        // Verify that expected method is called
        _orderRepo.Verify(r => r.GetByIdAsync(orderId), Times.Once());

        // Note: for ErrorOr, no need to assert result.Value not null
        // No error already implies it shouldn't be.
        Assert.False(result.IsError);
    }
    #endregion

    #region Create
    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task Create_ReturnsNotFound_WhenCustomerNotFound()
    {
        SetupCustomerExists(false);

        var requestDto = CreatePostRequest();
        var result = await _service.CreateAsync(requestDto);

        // Verify that exists method was invoked, but create method was not
        _customerRepo.Verify(r => r.ExistsAsync(requestDto.CustomerId), Times.Once());
        _orderRepo.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Never());

        // Assert that errors returned correctly
        Assert.True(result.IsError);
        Assert.Equal(ErrorCodes.CustomerNotFound, result.FirstError.Code);
    }

    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task Create_CallsRepoCorrectly_WhenCustomerFound()
    {
        SetupCustomerExists();

        var requestDto = CreatePostRequest();
        Order? capturedOrder = null!;

        // Setup create to capture the mapped order argument (reference)
        _orderRepo
            .Setup(r => r.CreateAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o)
            .ReturnsAsync(CreateOrder());

        var result = await _service.CreateAsync(requestDto);

        // Verify that exists and create methods were both invoked correctly
        // Note: CreateAsync is redundant, but verify to make explicit it's expected call
        _customerRepo.Verify(r => r.ExistsAsync(requestDto.CustomerId), Times.Once());
        _orderRepo.Verify(r => r.CreateAsync(capturedOrder), Times.Once());

        // Assert that no errors, and that request to order mapped correctly
        Assert.False(result.IsError);
        Assert.Equal(requestDto.CustomerId, capturedOrder?.CustomerId);
        Assert.Equal(requestDto.Items.Count, capturedOrder?.Items.Count);

        // Assert that item mapped correctly
        var expectedItem = requestDto.Items.First();
        var capturedItem = capturedOrder?.Items.First();

        Assert.Equal(expectedItem.ProductName, capturedItem?.ProductName);
        Assert.Equal(expectedItem.Quantity, capturedItem?.Quantity);
        Assert.Equal(expectedItem.UnitPrice, capturedItem?.UnitPrice);
    }
    #endregion

    #region Update
    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task Update_ReturnsNotFound_WhenOrderNotFound()
    {
        // GetById is setup, because it is used by UpdateStatusAsync for exists check
        SetupGetById();

        int orderId = 1;
        var result = await _service.UpdateStatusAsync(orderId, OrderStatus.Scheduled);

        // Verify that get method called, but update is not
        _orderRepo.Verify(r => r.GetByIdAsync(orderId), Times.Once());
        _orderRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never());

        Assert.True(result.IsError);
        Assert.Equal(ErrorCodes.OrderNotFound, result.FirstError.Code);
    }

    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task Update_CallsRepoCorrectly_WhenOrderFound()
    {
        int orderId = 1;
        var order = CreateOrder(id: orderId);

        SetupGetById(order);

        // Capture the order passed into 'Update';
        Order? capturedOrder = null!;

        _orderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o)
            .Returns(Task.CompletedTask);

        var targetStatus = OrderStatus.Scheduled;
        var result = await _service.UpdateStatusAsync(orderId, targetStatus);

        // Verify that get and update methods both called
        // Note: UpdateAsync verify is redundant, but kept to be explicit that it's expected
        _orderRepo.Verify(r => r.GetByIdAsync(orderId), Times.Once());
        _orderRepo.Verify(r => r.UpdateAsync(capturedOrder), Times.Once());

        // Assert no error, and that status mutated successfully
        // Note: 'Same' asserts that object returned by get is passed to update
        Assert.False(result.IsError);
        Assert.Same(order, capturedOrder);
        Assert.Equal(targetStatus, capturedOrder?.Status);
    }
    #endregion

    #region Delete
    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task Delete_ReturnsNotFound_WhenOrderNotFound()
    {
        // GetById is setup, because it is used by DeleteAsync for exists check
        SetupGetById();

        int orderId = 1;
        var result = await _service.DeleteAsync(orderId);

        // Verify that get method called, but delete is not
        _orderRepo.Verify(r => r.GetByIdAsync(orderId), Times.Once());
        _orderRepo.Verify(r => r.DeleteAsync(It.IsAny<Order>()), Times.Never());

        Assert.True(result.IsError);
        Assert.Equal(ErrorCodes.OrderNotFound, result.FirstError.Code);
    }

    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task Delete_CallsRepoCorrectly_WhenOrderFound()
    {
        int orderId = 1;
        var order = CreateOrder(id: orderId);

        SetupGetById(order);

        // Capture the order passed into 'Delete';
        Order? capturedOrder = null!;

        _orderRepo
            .Setup(r => r.DeleteAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o)
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(orderId);

        // Verify that get and delete methods both called correcly
        _orderRepo.Verify(r => r.GetByIdAsync(orderId), Times.Once());
        _orderRepo.Verify(r => r.DeleteAsync(capturedOrder), Times.Once());

        // Assert no error, and that order from get is passed to delete
        Assert.False(result.IsError);
        Assert.Equal(result.Value, Result.Deleted);
        Assert.Same(order, capturedOrder);
    }
    #endregion

    #region MapToDto
    [Fact]
    [Layer("Service")]
    [Scope("Order")]
    public async Task MapToDto_MapsCorrectly()
    {
        var order = CreateOrder();
        var orderResponse = OrderService.MapToDto(order);

        // Assert that values mapped correctly
        Assert.Equal(order.Id, orderResponse.Id);
        Assert.Equal(order.Status.ToString(), orderResponse.Status);
        Assert.Equal(order.CustomerId, orderResponse.CustomerId);
        Assert.Equal(order.Created, orderResponse.Created);
        Assert.Equal(order.Items.Count, orderResponse.Items.Count);

        var expectedItem = order.Items.First();
        var mappedItem = orderResponse.Items.First();

        Assert.Equal(expectedItem.ProductName, mappedItem.ProductName);
        Assert.Equal(expectedItem.Quantity, mappedItem.Quantity);
        Assert.Equal(expectedItem.UnitPrice, mappedItem.UnitPrice);
    }
    #endregion
}
