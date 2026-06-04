using OrderManagement.Domain.Entities;

namespace OrderManagement.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(int page, int pageSize);
    Task<Customer?> GetCustomerIdAsync(int id);
    Task<Customer> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(int id);
}
