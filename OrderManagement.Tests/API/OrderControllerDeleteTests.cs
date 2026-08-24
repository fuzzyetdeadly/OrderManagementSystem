using OrderManagement.Tests.Common;
using System.Net;

namespace OrderManagement.Tests.API
{
    public class OrderControllerDeleteTests : OrderControllerTestsBase
    {
        public OrderControllerDeleteTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory, output) { }

        #region helpers
        private async Task<HttpResponseMessage> DeleteOrderViaApiAsync(int orderId)
        {
            return await _client
                .DeleteAsync($"/api/orders/{orderId}", TestContext.Current.CancellationToken);
        }
        #endregion

        #region Delete
        [Theory]
        [Layer("Api")]
        [Scope("Order")]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Delete_ReturnsNotFound_ForInvalidId(int invalidId)
        {
            // Act: send DELETE request to delete invalid order
            var response = await DeleteOrderViaApiAsync(invalidId);

            // Assert: correct status code returned
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task Delete_ReturnsBadRequest_ForNonNumericId()
        {
            // Act: send DELETE request to delete order with non-numeric ID
            var response = await _client
                .DeleteAsync($"/api/orders/a", TestContext.Current.CancellationToken);

            // Assert: correct status code returned
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task Delete_ReturnsNoContent_ForValidId()
        {
            // Arrange: create an order to delete
            var (_, order) = await CreateOrderViaApiAsync();

            // Act: send DELETE request to delete the created order
            var response = await DeleteOrderViaApiAsync(order.Id);

            // Assert: correct status code returned
            // Note: expect content length to be 0 is redundant,
            // as 'NoContent()' controller result implies no body
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task Delete_ReturnsNotFound_ForRepeatedDelete()
        {
            // Arrange: create an order to delete
            var (_, order) = await CreateOrderViaApiAsync();

            // Act: delete twice, expect second delete to return NotFound
            await DeleteOrderViaApiAsync(order.Id);
            
            var response = await DeleteOrderViaApiAsync(order.Id);

            // Assert: correct status code returned
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [Layer("Api")]
        [Scope("Order")]
        public async Task Delete_ResultsInNotFound_OnSubsequentGet()
        {
            // Arrange: create an order to delete
            var (_, order) = await CreateOrderViaApiAsync();

            // Act: delete the order
            await DeleteOrderViaApiAsync(order.Id);

            // Act: attempt to get the deleted order by ID
            var response = await _client
                .GetAsync($"/api/orders/{order.Id}", TestContext.Current.CancellationToken);

            // Assert: correct status code returned
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        #endregion
    }
}
