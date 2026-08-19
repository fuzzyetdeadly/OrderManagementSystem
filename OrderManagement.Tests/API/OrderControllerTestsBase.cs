using Microsoft.Extensions.DependencyInjection;
using OrderManagement.API.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;

namespace OrderManagement.Tests.API;

public abstract class OrderControllerTestsBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    private static bool _hasRunOnce = false;

    public OrderControllerTestsBase(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
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
    protected void SeedCustomer(int customerId = 1)
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

    protected static string GetOrdersRoute(string route = "api/orders", int page = 1, int pageSize = 20)
    {
        return $"{route}?page={page}&pageSize={pageSize}";
    }

    /// <summary>
    /// Creates an order via the API and returns the response and created order.
    /// If no DTO is provided, a default valid DTO will be used.
    /// </summary>
    /// <remarks>
    /// Should NOT be used for negative tests (will always fail the status check).
    /// </remarks>
    /// <param name="dto">The CreateOrderDto to use for creating the order. If null, a default valid DTO will be used.</param>
    /// <returns>A tuple containing the HttpResponseMessage and the created OrderResponse.</returns>
    protected async Task<(HttpResponseMessage Response, OrderResponse Order)> CreateOrderViaApiAsync(CreateOrderDto? dto = null)
    {
        // Arrange: create an order DTO with valid data
        dto ??= new CreateOrderDto
        {
            CustomerId = 1,
            Items = [new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m }]
        };

        // Act: send POST request to create order
        var response = await _client.PostAsJsonAsync("api/orders", dto, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Act: read the created order from the response
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.Current.CancellationToken);

        Assert.NotNull(order);

        return (response, order);
    }

    // Overload to set only customerId and a default valid order dto
    private Task<(HttpResponseMessage Response, OrderResponse Order)> CreateOrderViaApiAsync(int customerId)
        => CreateOrderViaApiAsync(new CreateOrderDto
        {
            CustomerId = customerId,
            Items = [new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m }]
        });

    protected async Task CreateOrdersViaApiAsync(int count, int customerId = 1)
    {
        for (var i = 0; i < count; i++)
        {
            await CreateOrderViaApiAsync(customerId);
        }
    }

    protected static async Task AssertOk(HttpResponseMessage response)
    {
        // Ensure status ok before continue (prevent unhandled exception)
        var uri = response.RequestMessage?.RequestUri;
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.True(HttpStatusCode.OK == response.StatusCode, 
            $"AssertOk: Expected status OK, but got status {response.StatusCode}. Uri: {uri}, Content = {content}");
    }

    protected static IEnumerable<(string, int, decimal)> OrderItemDtosToTuples(IEnumerable<CreateOrderItemDto> items) 
        => items.Select(i => (i.ProductName!, i.Quantity!.Value, i.UnitPrice!.Value));

    protected static IEnumerable<(string, int, decimal)> OrderResponseItemsToTuples(IEnumerable<OrderItemResponse> items)
        => items.Select(i => (i.ProductName, i.Quantity, i.UnitPrice));
    #endregion
}
