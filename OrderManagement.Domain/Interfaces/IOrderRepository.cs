using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Domain.Interfaces;

/* Reminder:
 * Repositories represent implementations that allow services
 * to interface with the DBContext. It is also used for
 * testability, to mock repository responses.
 * Methods should always be async for scalability.
 * This ensures no process locking when DB actions take awhile
 */
public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, Pagination pagination);
}
