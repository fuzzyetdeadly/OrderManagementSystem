using Microsoft.Extensions.DependencyInjection;
using OrderManagement.API.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Tests.Common;
using System.Net;
using System.Net.Http.Json;

namespace OrderManagement.Tests.API;

public class OrderControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static bool _hasRunOnce = false;

    public OrderControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region interface methods
    // Runs before each test, start with clean database
    public async ValueTask InitializeAsync()
    {
        // Reset not required for first run test
        if (_hasRunOnce)
        {
            _factory.ResetDatabase();
        }
        else
        {
            _hasRunOnce = true;
        }

        // (Re)-seed default customer
        SeedCustomer();
    }

    public async ValueTask DisposeAsync()
    {
        // IDE suggestion: tell garbage collector this is already cleaned up,
        // no need to queue for finalization (wasted work).
        GC.SuppressFinalize(this);
    }
    #endregion

    #region helpers
    private void SeedCustomer(int customerId = 1)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Only create customer if they don't already exist
        if(!context.Customers.Any(c => c.Id == customerId)) 
        {
            context.Customers.Add(new Customer() 
            { 
                Id = customerId,
                Name = $"Customer{customerId:D2}",
                Email = $"Customer{customerId:D2}@noreply.com"
            });

            context.SaveChanges();
        }
    }

    private async Task<OrderResponse> CreateOrderViaApiAsync(int customerId = 1)
    {
        var dto = new CreateOrderDto()
        {
            CustomerId = customerId,
            Items = [new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m }]
        };

        var response = await _client.PostAsJsonAsync("api/orders", dto);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }


    private async Task CreateOrdersViaApiAsync(int count, int customerId = 1)
    {
        for (var i = 0; i < count; i++)
        {
            await CreateOrderViaApiAsync(customerId);
        }
    }

    private static string GetOrdersRoute(string route = "api/orders", int page = 1, int pageSize = 20) 
    {
        return $"{route}?page={page}&pageSize={pageSize}";
    }

    private static async Task AssertOk(HttpResponseMessage response)
    {
        // Ensure status ok before continue (prevent unhandled exception)
        var uri = response.RequestMessage?.RequestUri;
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.True(HttpStatusCode.OK == response.StatusCode, $"Uri: {uri}, Content = {content}");
    }
    #endregion

    #region GetAll
    [Fact]
    [Layer("Api")]
    [Scope("Order")]
    public async Task GetAll_ReturnsCorrectly_WhenNoOrders()
    {
        var response = await _client
            .GetAsync(GetOrdersRoute(), TestContext.Current.CancellationToken);

        await AssertOk(response);

        var orders = await response.Content
            .ReadFromJsonAsync<List<OrderResponse>>(TestContext.Current.CancellationToken);

        // Assert: no orders
        Assert.NotNull(orders);
        Assert.Empty(orders);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(1, 2, 2)]
    [InlineData(2, 2, 1)]
    [InlineData(3, 2, 0)]
    public async Task GetAll_ReturnsCorrectSlice_ForCompleteQuery(int page, int pageSize, int expectedCount)
    {
        await CreateOrdersViaApiAsync(3);

        // Note: page size 1 is NOT supported (covered in another test)
        var response = await _client
            .GetAsync(GetOrdersRoute(page: page, pageSize: pageSize), TestContext.Current.CancellationToken);

        await AssertOk(response);

        var orders = await response.Content
            .ReadFromJsonAsync<List<OrderResponse>>(TestContext.Current.CancellationToken);

        // Assert: orders match prescribed count
        Assert.NotNull(orders);
        Assert.Equal(expectedCount, orders.Count);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(10, 10)]
    [InlineData(20, 20)]
    [InlineData(30, 21)]
    public async Task GetAll_ReturnsCorrectly_ForDefaultPage(int pageSize, int expectedCount)
    {
        await CreateOrdersViaApiAsync(21);

        // Assumption: default page = 1
        string route = $"api/orders?pageSize={pageSize}";
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);

        await AssertOk(response);

        var orders = await response.Content
            .ReadFromJsonAsync<List<OrderResponse>>(TestContext.Current.CancellationToken);

        // Assert: orders match prescribed count
        Assert.NotNull(orders);
        Assert.Equal(expectedCount, orders.Count);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(1, 20)]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    public async Task GetAll_ReturnsCorrectly_ForDefaultPageSize(int page, int expectedCount)
    {
        await CreateOrdersViaApiAsync(21);

        // Assumption: default pageSize = 20
        string route = $"api/orders?page={page}";
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);

        await AssertOk(response);

        var orders = await response.Content
            .ReadFromJsonAsync<List<OrderResponse>>(TestContext.Current.CancellationToken);

        // Assert: orders match prescribed count
        Assert.NotNull(orders);
        Assert.Equal(expectedCount, orders.Count);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetAll_ReturnsBadRequest_ForInvalidPage(int page)
    {
        string route = $"api/orders?page={page}";
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetAll_ReturnsBadRequest_ForInvalidPageSize(int pageSize)
    {
        string route = $"api/orders?pageSize={pageSize}";
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    #endregion

    #region GetByCustomerId
    [Fact]
    [Layer("Api")]
    [Scope("Order")]
    public async Task GetByCustomerId_ReturnsCorrectly_WhenNoOrders()
    {
        var response = await _client
            .GetAsync(GetOrdersRoute(route: "api/customers/1/orders"), TestContext.Current.CancellationToken);
        
        await AssertOk(response);
        
        var orders = await response.Content
            .ReadFromJsonAsync<List<OrderResponse>>(TestContext.Current.CancellationToken);

        // Assert: no orders
        Assert.NotNull(orders);
        Assert.Empty(orders);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(1, 2, 2)]
    [InlineData(2, 2, 1)]
    [InlineData(3, 2, 0)]
    public async Task GetByCustomerId_ReturnsCorrectly_ForCompleteQuery(int page, int pageSize, int expectedCount)
    {
        // Arrange: create customer 2 and orders for both customers
        // Note: customer 1 is always seeded by default
        SeedCustomer(2);
        await CreateOrdersViaApiAsync(2, customerId: 1);
        await CreateOrdersViaApiAsync(3, customerId: 2);

        // Act: query for customer 2 orders
        var route = GetOrdersRoute(route: $"api/customers/2/orders", page: page, pageSize: pageSize);
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);

        await AssertOk(response);

        var orders = await response.Content
            .ReadFromJsonAsync<List<OrderResponse>>(TestContext.Current.CancellationToken);

        // Assert: orders match prescribed count
        Assert.NotNull(orders);
        Assert.Equal(expectedCount, orders.Count);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(10, 10)]
    [InlineData(20, 20)]
    [InlineData(30, 21)]
    public async Task GetByCustomerId_ReturnsCorrectly_ForDefaultPage(int pageSize, int expectedCount)
    {
        // Arrange: create orders for customer 1
        await CreateOrdersViaApiAsync(21, customerId: 1);

        // Act: query customer 1 orders with default page (1) and specified pageSize
        string route = $"api/customers/1/orders?pageSize={pageSize}";
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);

        await AssertOk(response);

        var orders = await response.Content
            .ReadFromJsonAsync<List<OrderResponse>>(TestContext.Current.CancellationToken);

        // Assert: orders match prescribed count
        Assert.NotNull(orders);
        Assert.Equal(expectedCount, orders.Count);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(1, 20)]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    public async Task GetByCustomerId_ReturnsCorrectly_ForDefaultPageSize(int page, int expectedCount)
    {
        // Arrange: create orders for customer 1
        await CreateOrdersViaApiAsync(21, customerId: 1);

        // Act: query customer 1 orders with specified page and default pageSize (20)
        string route = $"api/customers/1/orders?page={page}";
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);

        await AssertOk(response);

        var orders = await response.Content
            .ReadFromJsonAsync<List<OrderResponse>>(TestContext.Current.CancellationToken);

        // Assert: orders match prescribed count
        Assert.NotNull(orders);
        Assert.Equal(expectedCount, orders.Count);
    }

    [Fact]
    [Layer("Api")]
    [Scope("Order")]
    public async Task GetByCustomerId_ReturnsNotFound_ForCustomerIdNotFound()
    {
        string route = "api/customers/2/orders";
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetByCustomerId_ReturnsBadRequest_ForInvalidPage(int page)
    {
        string route = $"api/customers/1/orders?page={page}";
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetByCustomerId_ReturnsBadRequest_ForInvalidPageSize(int pageSize)
    {
        string route = $"api/customers/1/orders?pageSize={pageSize}";
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    #endregion
}
