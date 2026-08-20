using OrderManagement.API.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Entities;
using OrderManagement.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace OrderManagement.Tests.API
{
    public class OrderControllerPatchTests : OrderControllerTestsBase
    {
        public OrderControllerPatchTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory, output) { }

        #region UpdateStatus
        [Theory]
        [Layer("Api")]
        [Scope("Order")]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task UpdateStatus_ReturnsNotFound_WhenOrderDoesNotExist(int nonExistentOrderId)
        {
            // Arrange: valid dto
            var dto = new UpdateOrderStatusDto { Status = OrderStatus.Processing };

            // Act: send PATCH request to update status of non-existent order
            var response = await _client
                .PatchAsJsonAsync($"/api/orders/{nonExistentOrderId}/status", dto, TestContext.Current.CancellationToken);

            // Assert: correct status code returned
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task UpdateStatus_ReturnsBadRequest_ForNonNumericId()
        {
            // Arrange: valid dto
            var dto = new UpdateOrderStatusDto { Status = OrderStatus.Processing };

            // Act: send PATCH request to update status of non-existent order
            var response = await _client
                .PatchAsJsonAsync($"/api/orders/a/status", dto, TestContext.Current.CancellationToken);

            // Assert: correct status code returned
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [Layer("Api")]
        [Scope("Order")]
        [InlineData(null)]
        [InlineData((OrderStatus)999)]
        public async Task UpdateStatus_ReturnsBadRequest_ForInvalidStatus(OrderStatus? invalidStatus)
        {
            // Arrange: create order and a DTO with an invalid status
            var (_, order) = await CreateOrderViaApiAsync();

            var dto = new UpdateOrderStatusDto { Status = invalidStatus };

            // Act: send PATCH request with invalid status
            var response = await _client
                .PatchAsJsonAsync($"/api/orders/{order.Id}/status", dto, TestContext.Current.CancellationToken);

            // Assert: correct status code returned
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task UpdateStatus_ReturnsBadRequest_ForMalformedJsonBody()
        {
            // Arrange: create order and a malformed JSON body
            var (_, order) = await CreateOrderViaApiAsync();

            var malformedContent = new StringContent("{ \"Status\": ", encoding: Encoding.UTF8, "application/json");

            // Act: send PATCH request with malformed JSON
            var response = await _client
                .PatchAsync($"/api/orders/{order.Id}/status", malformedContent, TestContext.Current.CancellationToken);

            // Assert: correct status code returned with problem-details response
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task UpdateStatus_ReturnsUpdatedOrder_WhenStatusIsValid()
        {
            // Arrange: create order and a valid DTO
            var (_, order) = await CreateOrderViaApiAsync();

            var dto = new UpdateOrderStatusDto { Status = OrderStatus.Scheduled };

            // Act: send PATCH request to update status
            var response = await _client
                .PatchAsJsonAsync($"/api/orders/{order.Id}/status", dto, TestContext.Current.CancellationToken);

            // Assert: correct status code returned and only order status updated
            // i.e. no other fields should be changed, and items should remain the same
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var updatedOrder = await response.Content
                .ReadFromJsonAsync<OrderResponse>(TestContext.Current.CancellationToken);
            
            Assert.NotNull(updatedOrder);
            Assert.Equal(order.Id, updatedOrder.Id);
            Assert.Equal(order.CustomerId, updatedOrder.CustomerId);
            Assert.Equal(dto.Status.ToString(), updatedOrder.Status);
            Assert.Equal(
                OrderResponseItemsToTuples(order.Items),
                OrderResponseItemsToTuples(updatedOrder.Items)
            );
        }
        #endregion
    }
}
