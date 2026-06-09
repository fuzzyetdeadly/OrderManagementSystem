using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(Pagination pagination)
    {
        // Note: 'Include' order items is required to ensure navigable items
        // are also accessible with the returned data.
        // The Select here is also used to map data without Customer
        // Otherwise, 
        return await _context.Customers
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();
    }

    public async Task<Customer?> GetCustomerIdAsync(int id)
    {
        // Returns an Customer if found, with Customer/OrderItem navigations
        return await _context.Customers
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<bool> ExistsAsync(int id) => 
        await _context.Customers.AnyAsync(c => c.Id == id);

    public async Task<Customer> CreateAsync(Customer customer)
    {
        // Add the customer, the save it
        // Note: DB is configured to set 'Created/Updated' times
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    /* Note: 
     * Mutation of details is owned by service layer
     * Mutation of Updated time is owned by repository layer 
     * Throw to fail fast if customer is null
     * (to prevent need for cascading null checks)
     */
    public async Task UpdateAsync(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        customer.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Customer customer)
    {
        // Expect a valid customer from consumer
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }
}
