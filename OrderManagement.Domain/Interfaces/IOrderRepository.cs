using OrderManagement.Domain.Entities;

namespace OrderManagement.Domain.Interfaces;

/* Reminder:
 * Repositories represent implementations that allow services
 * to interface with the DBContext. It is also used for
 * testability, to mock repository responses.
 * Methods should always be async for scalability.
 * This ensures no process locking when DB actions take awhile
 */
public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync(int page, int pageSize);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, int page, int pageSize);
    Task<Order?> GetOrderIdAsync(int id);
    Task<Order> CreateAsync(Order order);
    Task UpdateAsync(Order order);
    Task<bool> DeleteAsync(int id);
}
