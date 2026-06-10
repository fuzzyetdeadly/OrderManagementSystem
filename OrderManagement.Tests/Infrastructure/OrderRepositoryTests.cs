using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Repositories;
using OrderManagement.Tests.Common;

namespace OrderManagement.Tests.Infrastructure;

/*
 * Repositories normally talk to databases
 * Instead of mocking, an SQLite DB is used (supports FK constraints).
 */
public class OrderRepositoryTests : RepositoryTestsBase<OrderRepository, Order>
{
    // Fields
    // Customer is set by SeedInitialDataSync
    private Customer _customer = null!;

    #region overrides
    protected override OrderRepository CreateRepository(AppDbContext context) => new(context);

    protected override async Task SeedInitialDataAsync()
    {
        _customer = await SeedCustomerAsync();
    }
    #endregion

    #region helpers
    /*
     * Creates a template order for a customer
     */
    private async Task<Order> SeedOrderAsync(int customerId = 1)
    {
        var order = new Order()
        {
            CustomerId = customerId,
            Items = [new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m }]
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }
    #endregion

    #region GetAll
    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoOrders()
    {
        var result = await _repository.GetAllAsync(_pagination);

        Assert.Empty(result);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task GetAllAsync_ReturnsPagedOrders()
    {
        await SeedOrderAsync();
        await SeedOrderAsync();
        await SeedOrderAsync();

        var pagination = new Pagination(Page: 1, PageSize: 2);
        var result = await _repository.GetAllAsync(pagination);

        Assert.Equal(2, result.Count);
    }
    #endregion

    #region GetByCustomerId
    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task GetByCustomerIdAsync_ReturnsEmpty_WhenInvalidCustomer()
    {
        var result = await _repository.GetByCustomerIdAsync(0, _pagination);

        Assert.Empty(result);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task GetByCustomerIdAsync_ReturnsEmpty_WhenNoOrders()
    {
        var result = await _repository.GetByCustomerIdAsync(_customer.Id, _pagination);

        Assert.Empty(result);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task GetByCustomerIdAsync_ReturnsMatchingOrders()
    {
        // An extra customer is needed for this test
        await SeedCustomerAsync();

        await SeedOrderAsync(customerId: 1);
        await SeedOrderAsync(customerId: 1);
        await SeedOrderAsync(customerId: 2);

        var result = await _repository.GetByCustomerIdAsync(_customer.Id, _pagination);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task GetByCustomerIdAsync_ReturnsMatchingPagedOrders()
    {
        // An extra customer is needed for this test
        await SeedCustomerAsync();

        await SeedOrderAsync(customerId: 1);
        await SeedOrderAsync(customerId: 1);

        var pagination = new Pagination(Page: 1, PageSize: 1);
        var result = await _repository.GetByCustomerIdAsync(_customer.Id, pagination);

        Assert.Single(result);
    }
    #endregion

    #region GetOrderId
    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task GetOrderId_ReturnsNull_WhenInvalidOrder()
    {
        var result = await _repository.GetByIdAsync(0);

        Assert.Null(result);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task GetOrderId_ReturnsOrder()
    {
        var seededOrder = await SeedOrderAsync();

        var result = await _repository.GetByIdAsync(1);

        // Assert that order returned isn't null with correct id
        Assert.NotNull(result);
        Assert.Equal(seededOrder.Id, result.Id);
    }
    #endregion

    #region Exists
    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task Exists_ReturnsFalse_WhenInvalidOrder()
    {
        var result = await _repository.ExistsAsync(0);

        Assert.False(result);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task Exists_ReturnsTrue_WhenValidOrder()
    {
        await SeedOrderAsync();

        var result = await _repository.ExistsAsync(1);

        Assert.True(result);
    }
    #endregion

    #region Create
    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task Create_ReturnsOrder()
    {
        var order = new Order()
        {
            CustomerId = _customer.Id,
            Items = [new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m }]
        };

        var result = await _repository.CreateAsync(order);

        // Assert that order not null and keys assigned correctly
        Assert.NotNull(result);
        Assert.Equal(_customer.Id, result.CustomerId);
        //Assert.True(result.Id > 0);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task Create_PersistsOrder()
    {
        var order = new Order()
        {
            CustomerId = _customer.Id,
            Items = [new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m }]
        };

        var result = await _repository.CreateAsync(order);

        // Clear the context cache, then try to get the persisted order
        ClearContext();

        // 'FindAsync' won't return 'Items' relation. 'Include is required'
        var persistedOrder = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == result.Id, TestContext.Current.CancellationToken);

        // Assert that order and its items persisted
        Assert.NotNull(persistedOrder);
        Assert.Single(persistedOrder.Items);
        Assert.True(persistedOrder.Items.First().Id > 0);
    }
    #endregion

    #region Update
    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task Update_PersistsChanges()
    {
        var seededOrder = await SeedOrderAsync();

        seededOrder.Status = OrderStatus.Scheduled;

        await _repository.UpdateAsync(seededOrder);

        // Clear the context cache, then try to get the persisted change
        ClearContext();

        // Cancellation token version of FindAsync requires seededOrder.Id to be in []
        // This is because the param accepted is 'object[] keyValues, CancellationToken'
        // Not wrapping [] causes other overload to be invoked with 'param object[] keyValues'
        var persistedOrder = await _context.Orders
            .FindAsync([seededOrder.Id], TestContext.Current.CancellationToken);

        // Assert that order and its status updated correctly
        Assert.NotNull(persistedOrder);
        Assert.Equal(OrderStatus.Scheduled, persistedOrder.Status);
    }
    #endregion

    #region Delete
    [Fact]
    [Layer("Repository")]
    [Scope("Order")]
    public async Task Delete_RemovesOrder()
    {
        var seededOrder = await SeedOrderAsync();

        await _repository.DeleteAsync(seededOrder);

        // Clear the context cache, then try to get the persisted change
        ClearContext();

        // Assert that order was removed
        var deletedOrder = await _context.Orders
            .FindAsync([seededOrder.Id], TestContext.Current.CancellationToken);

        Assert.Null(deletedOrder);
    }
    #endregion
}