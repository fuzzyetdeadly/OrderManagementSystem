using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.Infrastructure;

/*
 * Repositories normally talk to databases
 * Instead of mocking, an SQLite DB is used (supports FK constraints).
 * Previously tried InMemory, but FK constraints weren't honored.
 * For Generics, should validate TEntity before TRepository, since the
 * latter references it. Constraints are important for compile time safety
 */
public abstract class RepositoryTestsBase<TRepository, TEntity> : IAsyncLifetime
    where TEntity : IEntity
    where TRepository : IRepository<TEntity>
{
    // Constants
    private const string ConnectionString = "DataSource=:memory:";

    // Fields
    protected readonly static Pagination _pagination = new(Page: 1, PageSize: 20);
    private int _customerNo = 1;

    // Expect these instances to be set by 'InitializeAsync'
    // Generics can't have null assigned, use 'default' instead
    private SqliteConnection _connection = null!;
    protected AppDbContext _context = null!;
    protected TRepository _repository = default!;

    #region lifetime
    public async ValueTask InitializeAsync()
    {
        // Remember: connections need to be disposed of too!
        _connection = new SqliteConnection(ConnectionString);
        _connection.Open();

        // Initialize repository with SQLite memory database
        // A fresh instance is spun up per test
        // This is required because InMemory database doesn't enforce FK constraints
        // PendingModelChangesWarning is ignored, because it is incorrectly throwing
        // when attempting to create data.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        // Migrate the database, then create the repository
        _context = new AppDbContext(options);
        await _context.Database.MigrateAsync();
        _repository = CreateRepository(_context);

        // Seed any initial data that is required
        await SeedInitialDataAsync();
    }

    // Dispose will be run after each test to clean up context and memoryy
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();

        // IDE suggestion: tell garbage collector this is already cleaned up,
        // no need to queue for finalization (wasted work).
        GC.SuppressFinalize(this);
    }
    #endregion

    #region abstract
    // Note: it is not allowed to construct generic types with params
    // Therefore, construction must be done in a concrete class
    // Hint: body just needs to be 'return new(context)'
    protected abstract TRepository CreateRepository(AppDbContext context);
    protected abstract Task SeedInitialDataAsync();
    #endregion

    #region helpers
    protected async Task<Customer> SeedCustomerAsync()
    {
        // Create a dummy customer and increment number
        var customer = new Customer()
        {
            Name = $"Customer{_customerNo:D2}",
            Email = $"Customer{_customerNo:D2}@noreply.com"
        };

        _customerNo++;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    // Certain tests require context to be cleared before assertion
    // Because the results from acting may persist in cache, causing false positives
    protected void ClearContext() => _context.ChangeTracker.Clear();
    #endregion
}
