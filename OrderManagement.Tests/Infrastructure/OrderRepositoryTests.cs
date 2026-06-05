using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Repositories;
using Xunit;

namespace OrderManagement.Tests.Infrastructure;

/*
 * Repositories normally talk to databases
 * Instead of mocking, EF Core's InMemory only DB is used.
 */
public class OrderRepositoryTests : IAsyncLifetime
{
    // Expect these instances to be set by 'InitializeAsync'
    private AppDbContext _context = null!;
    private OrderRepository _repository = null!;
    private Customer _customer = null!;

    public async Task InitializeAsync()
    {
        // Initialize repository with in-memory database
        // A fresh instance is spun up per test
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new OrderRepository(_context);

        // Always seed one customer
        _customer = await SeedCustomerAsync();
    }

    // Dispose will be run after each test to clean up the memory
    public async Task DisposeAsync() => await _context.DisposeAsync();

    #region helpers
    private async Task<Customer> SeedCustomerAsync()
    {
        var customer = new Customer()
        {
            Name = "Jane Doe",
            Email = "jane.doe@gmail.com"
        };

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
            Items = [ new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m } ]
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }
    #endregion

    #region tests
    [Fact]
    [Trait("Category", "Repository")]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoOrders()
    {
        var pagination = new Pagination(Page: 1, PageSize: 20);
        var result = await _repository.GetAllAsync(pagination);

        Assert.Empty(result);
    }
    #endregion
}
