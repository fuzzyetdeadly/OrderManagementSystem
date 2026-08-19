using OrderManagement.Application.Models;
using OrderManagement.Tests.Common;
using System.Net;
using System.Net.Http.Json;

namespace OrderManagement.Tests.API
{
    public class OrderControllerGetTests : OrderControllerTestsBase
    {
        public OrderControllerGetTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory, output) { }

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
            var (_, createdOrder) = await CreateOrderViaApiAsync();

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
    }
}
