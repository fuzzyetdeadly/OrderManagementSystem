using OrderManagement.Domain.Common;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Tests.Common
{
    // Simple throwing fake, place near the test class or in a shared test-helpers file
    public class MockOrderRepository : IOrderRepository
    {
        public Task<Order> CreateAsync(Order order) =>
            throw new InvalidOperationException("Simulated connection failure");

        public Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, Pagination pagination) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Order>> GetAllAsync(Pagination pagination) =>
            throw new NotImplementedException();

        public Task<Order?> GetByIdAsync(int id) =>
            throw new NotImplementedException();

        public Task<bool> ExistsAsync(int id) =>
            throw new NotImplementedException();

        public Task UpdateAsync(Order order) =>
            throw new NotImplementedException();

        public Task DeleteAsync(Order order) =>
            throw new NotImplementedException();
    }
}
