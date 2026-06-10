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
    public async Task GetAllAsync_ReturnsEmpty_WhenNoCustomers()
    {
        var result = await _repository.GetAllAsync(_pagination);

        Assert.Empty(result);
    }

    [Fact]
    [Layer("Repository")]
    [Scope("Customer")]
    public async Task GetAllAsync_ReturnsPagedCustomers()
    {
        await SeedCustomerAsync();
        await SeedCustomerAsync();
        await SeedCustomerAsync();

        var pagination = new Pagination(Page: 1, PageSize: 2);
        var result = await _repository.GetAllAsync(pagination);

        Assert.Equal(2, result.Count);
    }
    #endregion
}
