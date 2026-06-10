using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Repositories;
using OrderManagement.Tests.Common;

namespace OrderManagement.Tests.Infrastructure;

public class CustomerRepositoryTests : RepositoryTestsBase<CustomerRepository, Customer>
{
    #region overrides
    protected override CustomerRepository CreateRepository(AppDbContext context) => new(context);

    protected override async Task SeedInitialDataAsync()
    {
        // No data to seed
    }
    #endregion

    #region GetAll
    [Fact]
    [Layer("Repository")]
    [Scope("Customer")]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoData()
    {
        var result = await _repository.GetAllAsync(_pagination);

        Assert.Empty(result);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Customer")]
    public async Task GetAllAsync_RespectsPagination()
    {
        await SeedCustomerAsync();
        await SeedCustomerAsync();
        await SeedCustomerAsync();

        var pagination = new Pagination(Page: 1, PageSize: 2);
        var result = await _repository.GetAllAsync(pagination);

        Assert.Equal(2, result.Count);
    }
    #endregion

    #region GetById
    [Fact]
    [Layer("Repository")]
    [Scope("Customer")]
    public async Task GetById_ReturnsNull_WhenInvalidId()
    {
        var result = await _repository.GetByIdAsync(0);

        Assert.Null(result);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Customer")]
    public async Task GetById_ReturnsCustomer()
    {
        var seededCustomer = await SeedCustomerAsync();

        var result = await _repository.GetByIdAsync(seededCustomer.Id);

        // Assert that order returned isn't null with correct id
        Assert.NotNull(result);
        Assert.Equal(seededCustomer.Id, result.Id);
    }
    #endregion

    #region Exists
    [Fact]
    [Layer("Repository")]
    [Scope("Customer")]
    public async Task Exists_ReturnsFalse_WhenInvalidCustomer()
    {
        var result = await _repository.ExistsAsync(0);

        Assert.False(result);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Customer")]
    public async Task Exists_ReturnsTrue_WhenValidCustomer()
    {
        var seededCustomer = await SeedCustomerAsync();

        var result = await _repository.ExistsAsync(seededCustomer.Id);

        Assert.True(result);
    }
    #endregion

    #region Create
    [Fact]
    [Layer("Repository")]
    [Scope("Customer")]
    public async Task Create_ThrowsWhenNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _repository.CreateAsync(null!));
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Customer")]
    public async Task Create_ReturnsCustomer()
    {
        var customer = new Customer()
        {
            Name = "Customer1",
            Email = "Customer1@gmail.com"
        };

        var result = await _repository.CreateAsync(customer);

        // Assert that customer not null and keys assigned correctly
        // Also expect no assocaited orders for fresh customer
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Empty(result.Orders);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Customer")]
    public async Task Create_PersistsOrder()
    {
        var customer = new Customer()
        {
            Name = "Customer1",
            Email = "Customer1@gmail.com"
        };

        var result = await _repository.CreateAsync(customer);

        // Clear the context cache, then try to get the persisted order
        ClearContext();

        // 'FindAsync' won't return 'Items' relation. 'Include is required'
        var persistedOrder = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == result.Id, TestContext.Current.CancellationToken);

        // Assert that order and its items persisted
        Assert.NotNull(persistedOrder);
    }
    #endregion
}
