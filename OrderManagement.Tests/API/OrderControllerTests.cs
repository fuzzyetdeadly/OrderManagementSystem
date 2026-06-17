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

    private static string GetOrdersRoute(string route = "api/orders", int page = 1, int pageSize = 20) 
    {
        return $"{route}?page={page}&pageSize={pageSize}";
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

        var orders = await response.Content
            .ReadFromJsonAsync<List<OrderResponse>>(TestContext.Current.CancellationToken);

        // Status should be OK, and orders should be empty
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(orders!);
    }

    [Fact]
    [Layer("Api")]
    [Scope("Order")]
    public async Task GetAll_ReturnsCorrectly_WhenOrdersExist()
    {
        await CreateOrderViaApiAsync();

        var response = await _client
            .GetAsync(GetOrdersRoute(), TestContext.Current.CancellationToken);

        var orders = await response.Content
            .ReadFromJsonAsync<List<OrderResponse>>(TestContext.Current.CancellationToken);

        // Status should be OK, and orders should be empty
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(orders!);
    }
    #endregion
}
