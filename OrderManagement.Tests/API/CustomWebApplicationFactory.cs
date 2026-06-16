using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.API;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    // Constants
    private const string ConnectionString = "DataSource=:memory:";

    // Fields
    private SqliteConnection _connection = null!;
    protected AppDbContext _context = null!;

    public CustomWebApplicationFactory()
    {
        // Keep a connection alive for factory lifetime
        // Closing it would wipe the in-memory database
        _connection = new SqliteConnection(ConnectionString);
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Switch environment to testing
        // This is to prevent development data being by 'Program.cs'
        // when running in development mode
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real PostGreSQL DBContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if(descriptor != null)
                services.Remove(descriptor);

            // Add context using SQLite, ignoring model changes warning
            // (Prevent incorrect throws while attempting to create data - copied from OrderRepoTest)
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection)
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

            // Migrate the dataset
            // Note: 'GetRequiredService' expects a non-null return
            // while 'GetService' returns AppDbContext?
            // Since signature of this method is synchronous, Migrate is preferred
            // to be done synchronously
            using var scope = services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Database.Migrate();
        });
    }

    protected override void Dispose(bool disposing)
    {
        // No null check. Expect fail fast if connection null
        if(disposing)
            _connection.Dispose();

        base.Dispose(disposing);
    }
}
