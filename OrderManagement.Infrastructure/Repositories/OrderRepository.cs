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

    public async Task<IEnumerable<Order>> GetAllAsync(int page, int pageSize)
    {
        // Note: 'Include' order items is required to ensure navigable items
        // are also accessible with the returned data.
        // The Select here is also used to map data without Customer
        // Otherwise, 
        return await _context.Orders
            .Include(o => o.Items)
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
        // Add the order, the save it
        // Note: DB is configured to set 'Created' time
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> UpdateStatusAsync(int id, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) 
            return null;

        order.Status = status;
        await _context.SaveChangesAsync();
        return order;
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
