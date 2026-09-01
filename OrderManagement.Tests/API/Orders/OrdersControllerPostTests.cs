using Microsoft.AspNetCore.Mvc;
using OrderManagement.API.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Entities;
using OrderManagement.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace OrderManagement.Tests.API.Orders
{
    public class OrderControllerCreateTests : OrdersControllerTestsBase
    {
        public OrderControllerCreateTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory, output) { }

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
            var response = await PostOrderViaApiAsync(dto);

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
            var response = await PostOrderViaApiAsync(dto);

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
            var response = await PostOrderViaApiAsync(dto);

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
            var response = await PostOrderViaApiAsync(dto);

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
            var response = await PostOrderViaApiAsync(dto);

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
            var response = await PostOrderViaApiAsync(dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content
                .ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);

            // Assert: problem details returned with multiple validation errors
            Assert.NotNull(problem);

            var requiredFields = new[] { "CustomerId", "ProductName", "Quantity", "UnitPrice" };

            Assert.All(requiredFields, field =>
                Assert.Contains(problem.Errors.Keys, k => k.Contains(field)));
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task Create_ReturnsBadRequest_ForMalformedJsonBody()
        {
            // Arrange: truncated JSON to simulate malformed body
            var malformedContent = new StringContent(
                "{ \"customerId\": 1, \"items\": [ { \"productName\": \"Potato\" ",
                Encoding.UTF8,
                "application/json");

            // Act: send POST request with malformed body
            var response = await _client
                .PostAsync("api/orders", malformedContent, TestContext.Current.CancellationToken);

            // Assert: bad request due to malformed JSON, before validator or handler even runs
            // Also expect response details to be a problem-details response
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task Create_ReturnsBadRequest_ForValidJsonWrongShape()
        {
            // Arrange: JSON with wrong shape
            var wrongShapeContent = new StringContent(
                "{ \"unexpectedField\": true }",
                Encoding.UTF8,
                "application/json");

            // Act: send POST request with wrong shape JSON body
            var response = await _client
                .PostAsync("api/orders", wrongShapeContent, TestContext.Current.CancellationToken);

            // Assert: bad request due to valid JSON but wrong shape, before validator or handler even runs
            // Also expect response details to be a problem-details response
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
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

            var (_, createdOrder) = await CreateOrderViaApiAsync(dto);

            // Assert: created order returned with expected header and values
            Assert.NotNull(createdOrder);
            Assert.Equal(dto.CustomerId, createdOrder.CustomerId);
            Assert.Equal(OrderStatus.Pending.ToString(), createdOrder.Status);
            Assert.Single(createdOrder.Items);
            Assert.Equal(
                OrderItemDtosToTuples(dto.Items),
                OrderResponseItemsToTuples(createdOrder.Items)
            );
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task Create_ReturnsCreatedOrderWithMultipleItems_WhenValid()
        {
            // Arrange: create an order DTO with multiple items
            var dto = new CreateOrderDto()
            {
                CustomerId = 1,
                Items = [
                    new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m },
                new() { ProductName = "Tomato", Quantity = 2, UnitPrice = 1.49m }
                ]
            };

            var (_, createdOrder) = await CreateOrderViaApiAsync(dto);

            // Assert: created order returned with expected header and values
            Assert.NotNull(createdOrder);
            Assert.Equal(dto.CustomerId, createdOrder.CustomerId);
            Assert.Equal(2, createdOrder.Items.Count);
            Assert.Equal(OrderStatus.Pending.ToString(), createdOrder.Status);
            Assert.Equal(
                OrderItemDtosToTuples(dto.Items),
                OrderResponseItemsToTuples(createdOrder.Items)
            );
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task Create_ReturnsResolvableOrderLocation_WhenValid()
        {
            // Arrange: create an order DTO with valid data
            var dto = new CreateOrderDto()
            {
                CustomerId = 1,
                Items = [new() { ProductName = "Potato", Quantity = 1, UnitPrice = 0.99m }]
            };

            var (response, createdOrder) = await CreateOrderViaApiAsync(dto);

            // Assert: location can be resolved to get the created order
            Assert.NotNull(createdOrder);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal($"/api/orders/{createdOrder.Id}", response.Headers.Location.AbsolutePath);

            var orderResponse = await _client.GetAsync(response.Headers.Location, TestContext.Current.CancellationToken);

            // Assert: order retrieved successfully
            await AssertOk(orderResponse);

            var retrievedOrder = await orderResponse.Content
                .ReadFromJsonAsync<OrderResponse>(TestContext.Current.CancellationToken);

            Assert.NotNull(retrievedOrder);
            Assert.Equal(createdOrder.Id, retrievedOrder.Id);
            Assert.Equal(dto.CustomerId, retrievedOrder.CustomerId);
            Assert.Single(retrievedOrder.Items);
            Assert.Equal(OrderStatus.Pending.ToString(), retrievedOrder.Status);
            Assert.Equal(
                OrderItemDtosToTuples(dto.Items),
                OrderResponseItemsToTuples(retrievedOrder.Items)
            );
        }
        #endregion
    }
}
