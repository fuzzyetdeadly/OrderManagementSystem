using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Repositories;

namespace OrderManagement.Tests.Infrastructure;

/*
 * Repositories normally talk to databases
 * Instead of mocking, an SQLite DB is used (supports FK constraints).
 */
public class OrderRepositoryTests : IAsyncLifetime
{
    // Constants
    private readonly static Pagination _pagination = new(Page: 1, PageSize: 20);

    // Trackers
    private int _customerNo = 1;

    // Expect these instances to be set by 'InitializeAsync'
    private SqliteConnection _connection = null!;
    private AppDbContext _context = null!;
    private OrderRepository _repository = null!;
    private Customer _customer = null!;

    public async Task InitializeAsync()
    {
        // Remember: connections need to be disposed of too!
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Initialize repository with SQLite memory database
        // A fresh instance is spun up per test
        // This is required because InMemory database doesn't enforce FK constraints
        // PendingModelChangesWarning is ignored, because it is incorrectly throwing
        // when attempting to create data.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        // Migrate the database, then create the repository
        _context = new AppDbContext(options);
        await _context.Database.MigrateAsync();
        _repository = new OrderRepository(_context);

        // Always seed one customer
        _customer = await SeedCustomerAsync();
    }

    // Dispose will be run after each test to clean up context and memoryy
    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    #region helpers
    private async Task<Customer> SeedCustomerAsync()
    {
        // Create a dummy customer and increment number
        var customer = new Customer()
        {
            Name = $"Customer{_customerNo:D2}",
            Email = $"Customer{_customerNo:D2}@noreply.com"
        };

        _customerNo++;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

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

    // Certain tests require context to be cleared before assertion
    // Because the results from acting may persist in cache, causing false positives
    private void ClearContext() => _context.ChangeTracker.Clear();
    #endregion

    #region GetAll
    [Fact]
    [Trait("Category", "Repository")]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoOrders()
    {
        var result = await _repository.GetAllAsync(_pagination);

        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Repository")]
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
    [Trait("Category", "Repository")]
    public async Task GetByCustomerIdAsync_ReturnsEmpty_WhenInvalidCustomer()
    {
        var result = await _repository.GetByCustomerIdAsync(0, _pagination);

        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Repository")]
    public async Task GetByCustomerIdAsync_ReturnsEmpty_WhenNoOrders()
    {
        var result = await _repository.GetByCustomerIdAsync(_customer.Id, _pagination);

        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Repository")]
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
    [Trait("Category", "Repository")]
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
    [Trait("Category", "Repository")]
    public async Task GetOrderId_ReturnsNull_WhenInvalidOrder()
    {
        var result = await _repository.GetOrderIdAsync(0);

        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Repository")]
    public async Task GetOrderId_ReturnsOrder()
    {
        var seededOrder = await SeedOrderAsync();

        var result = await _repository.GetOrderIdAsync(1);

        // Assert that order returned isn't null with correct id
        Assert.NotNull(result);
        Assert.Equal(seededOrder.Id, result.Id);
    }
    #endregion

    #region Exists
    [Fact]
    [Trait("Category", "Repository")]
    public async Task Exists_ReturnsFalse_WhenInvalidOrder()
    {
        var result = await _repository.ExistsAsync(0);

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Repository")]
    public async Task Exists_ReturnsTrue_WhenValidOrder()
    {
        await SeedOrderAsync();

        var result = await _repository.ExistsAsync(1);

        Assert.True(result);
    }
    #endregion

    #region Create
    [Fact]
    [Trait("Category", "Repository")]
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
    [Trait("Category", "Repository")]
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
            .FirstOrDefaultAsync(o => o.Id == result.Id);

        // Assert that order and its items persisted
        Assert.NotNull(persistedOrder);
        Assert.Single(persistedOrder.Items);
        Assert.True(persistedOrder.Items.First().Id > 0);
    }
    #endregion

    #region Update
    [Fact]
    [Trait("Category", "Repository")]
    public async Task Update_PersistsChanges()
    {
        var seededOrder = await SeedOrderAsync();

        seededOrder.Status = OrderStatus.Scheduled;

        await _repository.UpdateAsync(seededOrder);

        // Clear the context cache, then try to get the persisted change
        ClearContext();

        var persistedOrder = await _context.Orders.FindAsync(seededOrder.Id);

        // Assert that order and its status updated correctly
        Assert.NotNull(persistedOrder);
        Assert.Equal(OrderStatus.Scheduled, persistedOrder.Status);
    }
    #endregion

    #region Delete
    [Fact]
    [Trait("Category", "Repository")]
    public async Task Delete_RemovesOrder()
    {
        var seededOrder = await SeedOrderAsync();

        await _repository.DeleteAsync(seededOrder);

        // Clear the context cache, then try to get the persisted change
        ClearContext();

        // Assert that order was removed
        var deletedOrder = await _context.Orders.FindAsync(seededOrder.Id);

        Assert.Null(deletedOrder);
    }
    #endregion
}