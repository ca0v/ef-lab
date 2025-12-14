using Microsoft.EntityFrameworkCore;
using EFLab.Data;

namespace EFLab.Testing;

/// <summary>
/// Helper for creating DbContext with different providers (InMemory, SQLite)
/// </summary>
public static class DatabaseProvider
{
    public enum Provider
    {
        InMemory,
        SQLite
    }

    private static Provider _currentProvider = Provider.InMemory;
    private static int _databaseCounter = 0;

    public static void SetProvider(Provider provider)
    {
        _currentProvider = provider;
    }

    public static Provider GetProvider()
    {
        return _currentProvider;
    }

    /// <summary>
    /// Creates DbContextOptions for the configured provider
    /// </summary>
    public static DbContextOptions<AppDbContext> CreateOptions(string testName)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        
        if (_currentProvider == Provider.SQLite)
        {
            // Use unique database file for each test
            var dbFile = $"test_{testName}_{_databaseCounter++}.db";
            builder.UseSqlite($"Data Source={dbFile}");
        }
        else
        {
            // InMemory - use test name as database name
            builder.UseInMemoryDatabase(databaseName: testName);
        }

        return builder.Options;
    }

    /// <summary>
    /// Ensures database is created (for SQLite) and returns a context
    /// </summary>
    public static AppDbContext CreateContext(string testName)
    {
        var options = CreateOptions(testName);
        var context = new AppDbContext(options);
        
        if (_currentProvider == Provider.SQLite)
        {
            // Ensure database and schema are created
            context.Database.EnsureCreated();
        }
        
        return context;
    }

    /// <summary>
    /// Creates a context and ensures database is created (if SQLite)
    /// Use this for the first context in a test to ensure schema exists
    /// </summary>
    public static AppDbContext CreateContextWithOptions(DbContextOptions<AppDbContext> options)
    {
        var context = new AppDbContext(options);
        
        if (_currentProvider == Provider.SQLite)
        {
            // Ensure database and schema are created
            context.Database.EnsureCreated();
        }
        
        return context;
    }

    /// <summary>
    /// Cleans up test database (for SQLite)
    /// </summary>
    public static void CleanupDatabase(AppDbContext context)
    {
        if (_currentProvider == Provider.SQLite)
        {
            context.Database.EnsureDeleted();
        }
        context.Dispose();
    }

    /// <summary>
    /// Gets a user-friendly name for the current provider
    /// </summary>
    public static string GetProviderName()
    {
        return _currentProvider == Provider.SQLite ? "SQLite" : "InMemory";
    }
}
