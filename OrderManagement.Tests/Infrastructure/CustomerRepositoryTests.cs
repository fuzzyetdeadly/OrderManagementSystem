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
}
