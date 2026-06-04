using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

/*
 * Repository implementation for EF Core
 * Reminder: AppDbContext is injected by services builder
 */
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(int page, int pageSize)
    {
        // Note: 'Include' order items is required to ensure navigable items
        // are also accessible with the returned data.
        // OrderBy is required because SQL doesn't guarantee row order
        return await _context.Orders
            .Include(o => o.Items)
            .OrderBy(o => o.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, int page, int pageSize)
    {
        return await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.Items)
            .OrderBy(o => o.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderIdAsync(int id)
    {
        // Returns an order if found, with Customer/OrderItem navigations
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        // Add the order, then save it
        // Note: DB is configured to set 'Created' time
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    // The update method just saves the latest state.
    // The order parameter is redundant, but kept to be explicit about what's updated
    public async Task UpdateAsync(Order order)
    {
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) 
            return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }
}
