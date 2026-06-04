using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(Pagination pagination);
    Task<Customer?> GetCustomerIdAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<Customer> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
}
