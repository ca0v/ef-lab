using Microsoft.EntityFrameworkCore;
using EFLab.Testing;
using EFLab.Models;
using EFLab.Data;

namespace EFLab.Tests;

/// <summary>
/// In-Memory Testing: Setup, advantages, and critical limitations
/// </summary>
public static class InMemoryTestingTests
{
    [Tutorial(
        title: "In-Memory Database Does Not Enforce Relational Constraints",
        category: "In-Memory Testing",
        concept: @"The EF Core In-Memory provider is great for fast tests, but it's NOT a relational database. It doesn't enforce many constraints that real databases do.

Key limitations:
- No foreign key constraints enforcement
- No unique constraints (other than primary keys)
- No check constraints
- No database-side computed columns
- No stored procedures or triggers
- Simpler query translation (some LINQ queries that work in-memory fail on real DBs)

The In-Memory provider is a fake that stores data in memory collections, not a real database engine.",
        pitfall: @"**Common Mistake:** Assuming tests that pass with In-Memory will work the same way with SQL Server, PostgreSQL, etc.

This test demonstrates:
1. Creating an OrderItem with an invalid ProductId (doesn't exist)
2. Saving it successfully with In-Memory database
3. This would FAIL with a real database (foreign key violation)

The test 'passes' with In-Memory but reveals the pitfall - your app could have bugs that only appear in production!",
        fix: @"**Solution:** Understand In-Memory limitations and supplement with integration tests:

```csharp
// For unit tests: In-Memory is fast and good enough
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(""test"")
    .Options;

// For integration tests: Use real database or SQLite
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(""DataSource=:memory:"")
    .Options;
```

SQLite in-memory mode enforces constraints but is still fast for testing.",
        order: 10
    )]
    public static void Test_InMemory_Does_Not_Enforce_Foreign_Keys()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_FK_Not_Enforced")
            .Options;

        using var context = new AppDbContext(options);

        // Create an order with an item referencing a non-existent product
        var order = new Order
        {
            CustomerName = "Test Customer",
            OrderDate = DateTime.Now,
            Items = new List<OrderItem>
            {
                new OrderItem 
                { 
                    ProductId = 99999, // This product doesn't exist!
                    Quantity = 1, 
                    PriceAtOrder = 100m 
                }
            }
        };

        context.Orders.Add(order);
        
        // With In-Memory: This succeeds (no foreign key check)
        // With real DB: This would throw DbUpdateException (FK violation)
        try
        {
            context.SaveChanges();
            
            // In-Memory allows this - but it's invalid data!
            var savedOrder = context.Orders.Include(o => o.Items).First();
            Assert.AreEqual(1, savedOrder.Items.Count, "In-Memory allowed saving an item with invalid ProductId");
            
            // This test demonstrates the limitation, but let's make it 'fail' to teach the lesson
            Assert.IsTrue(false, 
                "WARNING: In-Memory DB does not enforce foreign keys! " +
                "This would fail with a real database. Use SQLite for more realistic tests.");
        }
        catch (DbUpdateException)
        {
            // This is what SHOULD happen with a real database
            Assert.IsTrue(true, "Real database correctly rejected foreign key violation");
        }
    }

    [Tutorial(
        title: "In-Memory Database Does Not Persist Between Context Instances",
        category: "In-Memory Testing",
        concept: @"Each In-Memory database instance is identified by a database name string. The data persists in memory as long as:

1. At least one context using that database name exists, OR
2. You explicitly maintain a reference to keep it alive

Once all contexts are disposed and no root references exist, the data is garbage collected.

This is different from SQLite :memory: which persists for the connection lifetime.",
        pitfall: @"**Common Mistake:** Expecting In-Memory data to automatically persist across test methods or test classes without careful database name management.

This test demonstrates:
1. Creating a database with a unique name
2. Adding data and disposing the context
3. Creating a new context with the SAME database name
4. The data should still be there... but is it?

The test structure shows how database name management affects data persistence.",
        fix: @"**Solution:** Share database names carefully and manage context lifecycles:

```csharp
// Option 1: Use unique DB names per test (isolated)
UseInMemoryDatabase($""Test_{Guid.NewGuid()}"")

// Option 2: Share DB name across operations (persisted)
const string sharedDbName = ""SharedTestDb"";
UseInMemoryDatabase(sharedDbName)

// Option 3: Keep root context alive
var root = new AppDbContext(options);
// Data persists while root exists
```",
        order: 11
    )]
    public static void Test_InMemory_Persistence_Across_Contexts()
    {
        // Use a shared database name
        const string dbName = "Test_Shared_InMemory";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        // Step 1: Add data in first context
        using (var context = new AppDbContext(options))
        {
            context.Products.Add(new Product { Name = "Persistent Item", Price = 50m, Stock = 10 });
            context.SaveChanges();
        }

        // Step 2: Query in second context with SAME database name
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.FirstOrDefault(p => p.Name == "Persistent Item");
            
            // This SHOULD work because we used the same database name
            Assert.IsNotNull(product, "In-Memory data persists when using the same database name");
        }

        // Now test with a DIFFERENT database name
        var differentOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Different_DB_Name") // Different name!
            .Options;

        using (var context = new AppDbContext(differentOptions))
        {
            var product = context.Products.FirstOrDefault(p => p.Name == "Persistent Item");
            
            // This will be null - different database name = different database
            // BUG: This assertion expects data that doesn't exist in this database
            Assert.IsNotNull(product, 
                "Different database name = different database. Data does not magically transfer!");
        }
    }

    [Tutorial(
        title: "In-Memory Database Does Not Support Raw SQL Queries",
        category: "In-Memory Testing",
        concept: @"The In-Memory provider does not support executing raw SQL queries because there's no SQL engine. Methods like:

- FromSqlRaw()
- FromSqlInterpolated()
- ExecuteSqlRaw()
- ExecuteSqlInterpolated()

These will throw NotSupportedException with In-Memory provider.

This is a significant limitation if your application uses stored procedures, views, or optimized SQL queries.",
        pitfall: @"**Common Mistake:** Writing tests with In-Memory that never exercise your raw SQL code paths.

This test:
1. Attempts to use FromSqlRaw() to query products
2. With In-Memory, this throws NotSupportedException
3. Your tests pass without raw SQL, but production code uses it

The test FAILS to demonstrate that In-Memory can't test raw SQL scenarios.",
        fix: @"**Solution:** Use SQLite or real database for tests involving raw SQL:

```csharp
// Won't work with In-Memory:
var products = context.Products
    .FromSqlRaw(""SELECT * FROM Products WHERE Price > {0}"", 100)
    .ToList();

// For testing raw SQL, use SQLite:
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(""DataSource=:memory:"")
    .Options;

using var connection = new SqliteConnection(""DataSource=:memory:"");
connection.Open();
// Now raw SQL works!
```",
        order: 12
    )]
    public static void Test_InMemory_Does_Not_Support_Raw_SQL()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Raw_SQL")
            .Options;

        using var context = new AppDbContext(options);

        // Add some test data
        context.Products.Add(new Product { Name = "Expensive Item", Price = 500m, Stock = 5 });
        context.SaveChanges();

        // Try to use raw SQL (this will fail with In-Memory)
        try
        {
            // This line will throw NotSupportedException
            var products = context.Products
                .FromSqlRaw("SELECT * FROM Products WHERE Price > 100")
                .ToList();

            Assert.IsTrue(false, "Raw SQL should not work with In-Memory provider");
        }
        catch (InvalidOperationException)
        {
            // Expected with In-Memory provider
            // Make this test 'fail' to teach the lesson
            Assert.IsTrue(false,
                "In-Memory provider does not support raw SQL queries. " +
                "Use SQLite in-memory mode or real database for testing SQL.");
        }
    }

    [Tutorial(
        title: "In-Memory Database Query Translation Differs from Real Databases",
        category: "In-Memory Testing",
        concept: @"The In-Memory provider translates LINQ queries to in-memory operations, not SQL. Some queries that work in-memory will fail with real databases (and vice versa).

Differences include:
- Case sensitivity in string comparisons
- Client-side evaluation is more permissive
- Some SQL-specific functions don't exist in-memory
- Date/time operations may differ
- Collation and culture differences

Tests passing with In-Memory don't guarantee the LINQ will translate to valid SQL.",
        pitfall: @"**Common Mistake:** Using client-evaluated expressions that work in-memory but fail in production.

This test demonstrates:
1. A LINQ query using client-side method call
2. It works with In-Memory (evaluates in .NET)
3. With SQL Server, it might fail or behave differently

The test intentionally uses a pattern that highlights this difference.",
        fix: @"**Solution:** Be aware of query translation differences:

```csharp
// Might work in-memory but not in SQL:
.Where(p => IsExpensive(p.Price)) // Custom method

// Better - translatable to SQL:
.Where(p => p.Price > 100)

// Test with warnings enabled:
optionsBuilder
    .UseInMemoryDatabase(""test"")
    .ConfigureWarnings(w => w.Throw(
        InMemoryEventId.TransactionIgnoredWarning));
```

Consider using EF.Functions for database-specific operations.",
        order: 13
    )]
    public static void Test_InMemory_Query_Translation_Differences()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Query_Translation")
            .Options;

        using var context = new AppDbContext(options);

        // Add test data
        context.Products.AddRange(
            new Product { Name = "Cheap Item", Price = 10m, Stock = 100 },
            new Product { Name = "Expensive Item", Price = 1000m, Stock = 5 }
        );
        context.SaveChanges();

        // This query works with In-Memory (client evaluation)
        // But might not translate to SQL properly on a real database
        var expensiveProducts = context.Products
            .AsEnumerable() // Force client evaluation
            .Where(p => p.Name.Contains("Expensive", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.AreEqual(1, expensiveProducts.Count, 
            "In-Memory allows client evaluation, but this query pattern may not translate to SQL");

        // Make the test 'fail' to teach about query translation
        Assert.IsTrue(false,
            "WARNING: In-Memory query translation differs from real databases. " +
            "Use EnableSensitiveDataLogging and log SQL to verify query translation.");
    }
}
