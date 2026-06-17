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
    private readonly SqliteConnection _connection = null!;
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

    public void ResetDatabase()
    {
        // Get DB context
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get list of tables
        // * Distinct needed because some entity types may share a table name (in real world scenarios)
        // * Where needed as some Entities may not be backed by tables (in real world scenarios)
        var tableNames = context.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Distinct()
            .Where(name => !string.IsNullOrEmpty(name));

        // Discard records from each table, then reset table ID sequences
        foreach( var tableName in tableNames)
        {
            // Table names come from the EF model, not external input
            // So the warning is invalid in this context
            // Using 'ExecuteSql' also isn't valid here as identifiers can't be parameterized
#pragma warning  disable EF1002
            context.Database.ExecuteSqlRaw($"DELETE FROM \"{tableName}\";");
#pragma warning restore EF1002
        }

        context.Database.ExecuteSql($"DELETE FROM sqlite_squence;");
    }

    protected override void Dispose(bool disposing)
    {
        // No null check. Expect fail fast if connection null
        // _connection.Dispose will also invoke *.Close()
        if(disposing)
            _connection.Dispose();

        base.Dispose(disposing);
    }
}
