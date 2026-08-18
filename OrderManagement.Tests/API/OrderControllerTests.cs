using Microsoft.AspNetCore.Mvc;
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

        Assert.True(HttpStatusCode.OK == response.StatusCode, 
            $"AssertOk: Expected status OK, but got status {response.StatusCode}. Uri: {uri}, Content = {content}");
    }
    #endregion

    #region GetAll
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
    #endregion

    #region GetByCustomerId
    [Fact]
    [Layer("Api")]
    [Scope("Order")]
    public async Task GetByCustomerId_ReturnsNotFound_WhenCustomerNotFound()
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
    #endregion

    #region GetById
    [Fact]
    [Layer("Api")]
    [Scope("Order")]
    public async Task GetById_ReturnsNotFound_WhenOrderNotFound()
    {
        // Act: query for the order by id
        var route = $"api/orders/1";
        var response = await _client
            .GetAsync(route, TestContext.Current.CancellationToken);

        // Assert: order not found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Layer("Api")]
    [Scope("Order")]
    public async Task GetById_ReturnsCorrectly_WhenOrderExists()
    {
        // Arrange: create an order via API
        var createdOrder = await CreateOrderViaApiAsync();

        // Act: query for the order by id
        var response = await _client
            .GetAsync($"api/orders/{createdOrder.Id}", TestContext.Current.CancellationToken);

        await AssertOk(response);
        
        var order = await response.Content
            .ReadFromJsonAsync<OrderResponse>(TestContext.Current.CancellationToken);

        // Assert: order matches created order
        Assert.NotNull(order);
        Assert.Equal(createdOrder.Id, order.Id);
    }
    #endregion

    #region Create
    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(null, HttpStatusCode.BadRequest)]
    [InlineData(0, HttpStatusCode.BadRequest)]
    [InlineData(2, HttpStatusCode.NotFound)]
    public async Task Create_ReturnsExpectedStatus_ForCustomerIdIssues(int? customerId, HttpStatusCode expectedStatus)
    {
        // Arrange: create an order DTO with invalid data (missing CustomerId)
        var dto = new CreateOrderDto()
        {
            CustomerId = customerId,
            Items = [new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m }]
        };

        // Act: send POST request to create order
        var response = await _client.PostAsJsonAsync("api/orders", dto, TestContext.Current.CancellationToken);

        // Assert: bad request due to missing CustomerId
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    [Layer("Api")]
    [Scope("Order")]
    public async Task Create_ReturnsBadRequest_WhenItemsEmpty()
    {
        // Arrange: create an order DTO with a non-existent customerId
        // Note: items is empty by default when not set, but empty list is used to be explicity
        var dto = new CreateOrderDto()
        {
            CustomerId = 1,
            Items = []
        };

        // Act: send POST request to create order
        var response = await _client.PostAsJsonAsync("api/orders", dto, TestContext.Current.CancellationToken);

        // Assert: customer not found
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_ReturnsBadRequest_WhenItemProductNameInvalid(string? productName)
    {
        // Arrange: create an order DTO with an invalid product name
        var dto = new CreateOrderDto()
        {
            CustomerId = 1,
            Items = [new() { ProductName = productName, Quantity = 1, UnitPrice = 0.99m }]
        };

        // Act: send POST request to create order
        var response = await _client.PostAsJsonAsync("api/orders", dto, TestContext.Current.CancellationToken);

        // Assert: bad request due to invalid product name
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_ReturnsBadRequest_WhenItemQuantityInvalid(int? quantity)
    {
        // Arrange: create an order DTO with an invalid quantity
        var dto = new CreateOrderDto()
        {
            CustomerId = 1,
            Items = [new() { ProductName = "Potato", Quantity = quantity, UnitPrice = 0.99m }]
        };
        
        // Act: send POST request to create order
        var response = await _client.PostAsJsonAsync("api/orders", dto, TestContext.Current.CancellationToken);
        
        // Assert: bad request due to invalid quantity
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [Layer("Api")]
    [Scope("Order")]
    [InlineData(null)]
    [InlineData(0.00)]
    [InlineData(-0.01)]
    public async Task Create_ReturnsBadRequest_WhenItemPriceInvalid(double? unitPrice)
    {
        // Arrange: create an order DTO with an invalid price
        // Note: inline doesn't support decimal, so need to cast from double
        decimal? price = unitPrice.HasValue ? (decimal)unitPrice.Value : null;
        var dto = new CreateOrderDto()
        {
            CustomerId = 1,
            Items = [new() { ProductName = "Potato", Quantity = 1, UnitPrice = price }]
        };

        // Act: send POST request to create order
        var response = await _client.PostAsJsonAsync("api/orders", dto, TestContext.Current.CancellationToken);

        // Assert: bad request due to invalid price
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Layer("Api")]
    [Scope("Order")]
    public async Task Create_ReturnsAllValidationErrors_WhenMultipleFieldsInvalid()
    {
        // Arrange: create an order DTO with multiple invalid fields
        var dto = new CreateOrderDto()
        {
            CustomerId = null,
            Items = [new() { ProductName = null, Quantity = null, UnitPrice = null }]
        };

        // Act: send POST request to create order
        var response = await _client.PostAsJsonAsync("api/orders", dto, TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);

        // Assert: problem details returned with multiple validation errors
        Assert.NotNull(problem);

        var requiredFields = new[] { "CustomerId", "ProductName", "Quantity", "UnitPrice" };

        Assert.All(requiredFields, field =>
            Assert.Contains(problem.Errors.Keys, k => k.Contains(field)));
    }

    [Fact]
    [Layer("Api")]
    [Scope("Order")]
    public async Task Create_ReturnsCreatedOrder_WhenValid()
    {
        // Arrange: create an order DTO with valid data
        var dto = new CreateOrderDto()
        {
            CustomerId = 1,
            Items = [new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m }]
        };

        // Act: send POST request to create order
        var response = await _client.PostAsJsonAsync("api/orders", dto, TestContext.Current.CancellationToken);

        // Assert: order created successfully
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdOrder = await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.Current.CancellationToken);
        
        // Assert: created order returned with expected values
        Assert.NotNull(createdOrder);
        Assert.Equal(dto.CustomerId, createdOrder.CustomerId);
        Assert.Single(createdOrder.Items);
        Assert.Equal(dto.Items[0].ProductName, createdOrder.Items[0].ProductName);
        Assert.Equal(dto.Items[0].Quantity, createdOrder.Items[0].Quantity);
        Assert.Equal(dto.Items[0].UnitPrice, createdOrder.Items[0].UnitPrice);
    }
    #endregion

    #region UpdateStatus

    #endregion

    #region Delete

    #endregion
}
