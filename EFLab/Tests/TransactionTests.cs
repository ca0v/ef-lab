using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EFLab.Testing;
using EFLab.Models;
using EFLab.Data;

namespace EFLab.Tests;

/// <summary>
/// Transactions: Usage, nested transactions with sub-contexts, and common pitfalls
/// </summary>
public static class TransactionTests
{
    [Tutorial(
        title: "Transactions Require Explicit Commit",
        category: "Transactions",
        concept: @"EF Core supports explicit transactions through BeginTransaction(). This gives you control over when changes are committed or rolled back.

Key points:
- BeginTransaction() starts a transaction
- SaveChanges() writes to the database but doesn't commit the transaction
- Commit() makes changes permanent
- Rollback() or Dispose() without Commit() undoes all changes
- SaveChanges() within a transaction is atomic with Commit()

Without an explicit transaction, each SaveChanges() auto-commits.",
        pitfall: @"**Common Mistake:** Starting a transaction, calling SaveChanges(), but forgetting to Commit().

This test demonstrates:
1. BeginTransaction() starts a transaction
2. SaveChanges() executes SQL but doesn't commit
3. Transaction is disposed without Commit()
4. All changes are rolled back automatically

The test FAILS because the product was never committed to the database.",
        fix: @"**Solution:** Always call Commit() on transactions:

```csharp
using var transaction = context.Database.BeginTransaction();
try
{
    context.Products.Add(product);
    context.SaveChanges();
    transaction.Commit(); // Make it permanent!
}
catch
{
    transaction.Rollback(); // Or let Dispose() roll back
    throw;
}
```",
        order: 20
    )]
    public static void Test_Transaction_Requires_Commit()
    {
        var options = DatabaseProvider.CreateOptions("Test_Transaction_Commit");
        
        if (DatabaseProvider.GetProvider() == DatabaseProvider.Provider.InMemory)
        {
            // Suppress InMemory transaction warning
            options = new DbContextOptionsBuilder<AppDbContext>(options)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
        }

        // Act: Use transaction but don't commit
        using (var context = DatabaseProvider.CreateContextWithOptions(options))
        {
            using var transaction = context.Database.BeginTransaction();
            
            var product = new Product { Name = "Uncommitted Product", Price = 99.99m, Stock = 10 };
            context.Products.Add(product);
            context.SaveChanges();
            
            // BUG: Not calling transaction.Commit()!
            // Transaction will be rolled back on Dispose()
        }

        // Verify: Product should not exist
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.FirstOrDefault(p => p.Name == "Uncommitted Product");
            
            // This will FAIL - product was rolled back
            Assert.IsNotNull(product, "Product should exist after transaction.Commit()");
        }
    }

    [Tutorial(
        title: "Rollback Undoes All Changes in Transaction",
        category: "Transactions",
        concept: @"Transactions provide atomicity - either all changes succeed or all fail together. Rollback() explicitly undoes all changes made within the transaction.

Key points:
- Rollback() undoes all operations since BeginTransaction()
- Multiple SaveChanges() calls within a transaction are all rolled back
- Dispose() without Commit() implicitly rolls back
- Rollback is useful for error handling
- Changes are only visible within the transaction until committed",
        pitfall: @"**Common Mistake:** Not understanding that Rollback() undoes ALL changes, not just the last SaveChanges().

This test demonstrates:
1. Add multiple products in a transaction
2. SaveChanges() after each add
3. Explicitly call Rollback()
4. All products are gone, not just the last one

The test intentionally expects one product to remain (it won't).",
        fix: @"**Solution:** Understand rollback affects the entire transaction:

```csharp
using var transaction = context.Database.BeginTransaction();

context.Products.Add(product1);
context.SaveChanges(); // Executed but not committed

context.Products.Add(product2);
context.SaveChanges(); // Executed but not committed

transaction.Rollback(); // Both products are undone!
// Neither product exists in the database
```",
        order: 21
    )]
    public static void Test_Rollback_Undoes_All_Changes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Rollback_All")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using (var context = new AppDbContext(options))
        {
            using var transaction = context.Database.BeginTransaction();
            
            // Add first product
            context.Products.Add(new Product { Name = "Product 1", Price = 10m, Stock = 5 });
            context.SaveChanges();
            
            // Add second product
            context.Products.Add(new Product { Name = "Product 2", Price = 20m, Stock = 10 });
            context.SaveChanges();
            
            // Add third product
            context.Products.Add(new Product { Name = "Product 3", Price = 30m, Stock = 15 });
            context.SaveChanges();
            
            // Explicitly rollback
            transaction.Rollback();
        }

        // Verify: ALL products should be gone
        using (var context = new AppDbContext(options))
        {
            var count = context.Products.Count();
            
            // BUG: This assumes some products survived (they didn't!)
            Assert.AreEqual(2, count, 
                "Rollback undoes ALL changes in the transaction, not just the last one");
        }
    }

    [Tutorial(
        title: "Nested Transactions with Sub-Contexts Share Parent Transaction",
        category: "Transactions",
        concept: @"When you create a sub-context (child DbContext) and want it to participate in a parent's transaction, you must explicitly use UseTransaction() to share the transaction.

Key points:
- Each DbContext has its own connection by default
- Sub-contexts don't automatically share parent transactions
- Use context.Database.UseTransaction() to share a transaction
- Both contexts must use the same underlying database connection
- The parent transaction controls commit/rollback for all participating contexts",
        pitfall: @"**Common Mistake:** Creating a sub-context and assuming it automatically participates in the parent's transaction.

This test demonstrates:
1. Parent context starts a transaction
2. Parent adds a product and saves
3. Sub-context is created (without UseTransaction)
4. Sub-context tries to query the product
5. Product is not visible because sub-context is in a different transaction!

The test FAILS because the sub-context can't see uncommitted changes from the parent.",
        fix: @"**Solution:** Share the transaction explicitly:

```csharp
using var parentContext = new AppDbContext(options);
using var transaction = parentContext.Database.BeginTransaction();

parentContext.Products.Add(product);
parentContext.SaveChanges();

// Sub-context must use the same transaction
using var subContext = new AppDbContext(options);
subContext.Database.UseTransaction(transaction.GetDbTransaction());

// Now subContext can see parent's uncommitted changes
var product = subContext.Products.Find(id);
```",
        order: 22
    )]
    public static void Test_SubContext_Must_UseTransaction()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_SubContext_Transaction")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var parentContext = new AppDbContext(options);
        using var transaction = parentContext.Database.BeginTransaction();
        
        // Parent adds a product
        var product = new Product { Name = "Parent Product", Price = 50m, Stock = 20 };
        parentContext.Products.Add(product);
        parentContext.SaveChanges();
        
        // BUG: Create sub-context without sharing the transaction
        using (var subContext = new AppDbContext(options))
        {
            // Sub-context can't see parent's uncommitted changes
            var foundProduct = subContext.Products.FirstOrDefault(p => p.Name == "Parent Product");
            
            // This will FAIL - subContext is not in the same transaction
            Assert.IsNotNull(foundProduct, 
                "Sub-context must use UseTransaction() to see parent's uncommitted changes");
        }
        
        transaction.Rollback();
    }

    [Tutorial(
        title: "Partial Commits Don't Exist - Transaction is All or Nothing",
        category: "Transactions",
        concept: @"A transaction is atomic - you cannot partially commit some changes and rollback others. Once you commit, ALL changes in the transaction are permanent.

Key points:
- Commit() applies all SaveChanges() calls in the transaction
- You cannot selectively commit individual operations
- To have different commit boundaries, use separate transactions
- Nested transactions are not truly nested - they share the same underlying transaction
- Savepoints can provide partial rollback in some databases (advanced topic)",
        pitfall: @"**Common Mistake:** Thinking you can commit some changes and rollback others within the same transaction.

This test demonstrates:
1. Add multiple products in a transaction
2. Call SaveChanges() after each
3. Try to 'partially commit' by calling Commit() in the middle
4. Then try to rollback remaining changes

In reality, Commit() ends the transaction - you can't continue it!",
        fix: @"**Solution:** Use separate transactions for independent commit boundaries:

```csharp
// Transaction 1: Commit these changes
using (var transaction1 = context.Database.BeginTransaction())
{
    context.Products.Add(product1);
    context.SaveChanges();
    transaction1.Commit(); // These are permanent
}

// Transaction 2: Can rollback independently
using (var transaction2 = context.Database.BeginTransaction())
{
    context.Products.Add(product2);
    context.SaveChanges();
    transaction2.Rollback(); // Only affects transaction2
}
```",
        order: 23
    )]
    public static void Test_No_Partial_Commits_In_Transaction()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_No_Partial_Commit")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using (var context = new AppDbContext(options))
        {
            using var transaction = context.Database.BeginTransaction();
            
            // Add first product
            context.Products.Add(new Product { Name = "Committed Product", Price = 100m, Stock = 5 });
            context.SaveChanges();
            
            // Commit the transaction
            transaction.Commit();
            
            // BUG: Try to add more in the 'same' transaction (transaction is already committed!)
            try
            {
                context.Products.Add(new Product { Name = "After Commit", Price = 200m, Stock = 10 });
                context.SaveChanges();
                
                // This will succeed but is NOT in a transaction anymore!
                // It auto-commits immediately
            }
            catch (InvalidOperationException)
            {
                // Some providers might throw here
            }
        }

        // Verify: Both products exist (can't partially commit)
        using (var context = new AppDbContext(options))
        {
            var committedProduct = context.Products.FirstOrDefault(p => p.Name == "Committed Product");
            var afterCommitProduct = context.Products.FirstOrDefault(p => p.Name == "After Commit");
            
            Assert.IsNotNull(committedProduct, "First product should exist");
            
            // BUG: This assumes second product doesn't exist (it does - auto-committed!)
            Assert.IsNull(afterCommitProduct, 
                "After Commit() is called, the transaction ends. Subsequent SaveChanges() auto-commit!");
        }
    }

    [Tutorial(
        title: "Multiple Contexts with Shared Transaction Must Use Same Connection",
        category: "Transactions",
        concept: @"When multiple DbContext instances need to share a transaction, they must share the same underlying database connection. In-Memory provider handles this differently than relational databases.

Key points:
- Relational databases: Use the same DbConnection for all contexts
- In-Memory: Transactions are largely ignored (limitation!)
- For real databases, create connection first, then contexts
- All contexts must call UseTransaction() with the shared transaction
- The connection must remain open for the duration of the transaction",
        pitfall: @"**Common Mistake:** With In-Memory provider, transaction behavior differs from real databases.

This test demonstrates:
1. In-Memory provider doesn't truly support transactions across contexts
2. Transaction.Commit() and Rollback() are essentially no-ops
3. Changes are immediately visible regardless of transaction state

The test FAILS to highlight this In-Memory limitation.",
        fix: @"**Solution:** Use SQLite or real database for transaction testing:

```csharp
// For real database transaction testing:
using var connection = new SqliteConnection(""DataSource=:memory:"");
connection.Open();

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connection)
    .Options;

using var context1 = new AppDbContext(options);
using var transaction = context1.Database.BeginTransaction();

using var context2 = new AppDbContext(options);
context2.Database.UseTransaction(transaction.GetDbTransaction());

// Now both contexts share the same real transaction
```",
        order: 24
    )]
    public static void Test_InMemory_Transaction_Limitations()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Transaction_Limitation")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context1 = new AppDbContext(options);
        using var transaction = context1.Database.BeginTransaction();
        
        // Add product in transaction
        context1.Products.Add(new Product { Name = "Transaction Test", Price = 75m, Stock = 8 });
        context1.SaveChanges();
        
        // Create second context
        using (var context2 = new AppDbContext(options))
        {
            // In-Memory: Changes are visible immediately (transaction doesn't isolate)
            var product = context2.Products.FirstOrDefault(p => p.Name == "Transaction Test");
            
            // With In-Memory, this passes even though transaction not committed!
            // With real database, this would be null (transaction isolation)
            if (product != null)
            {
                Assert.IsTrue(false, 
                    "WARNING: In-Memory provider doesn't properly isolate transactions! " +
                    "Use SQLite or real database for accurate transaction testing.");
            }
        }
        
        transaction.Rollback();
        
        // After rollback, with In-Memory the data is still there (limitation)
        using (var context3 = new AppDbContext(options))
        {
            var product = context3.Products.FirstOrDefault(p => p.Name == "Transaction Test");
            
            // This will FAIL - In-Memory doesn't truly rollback
            Assert.IsNull(product, 
                "Product should be rolled back, but In-Memory provider doesn't support true transactions");
        }
    }

    [Tutorial(
        title: "Sharing Transactions Across Multiple Contexts (The RIGHT Way)",
        category: "Transactions",
        concept: @"YES, you can share a transaction across multiple DbContext instances! This is a common pattern when using:
- Repository pattern (different contexts for different aggregates)
- Service layers (each service gets its own context)
- Unit of Work pattern implementations
- Microservices coordination

**IMPORTANT: This requires a RELATIONAL database provider (SQL Server, PostgreSQL, SQLite, etc.)**
In-Memory provider does NOT support transaction sharing via GetDbTransaction().

Key steps:
1. Parent context starts the transaction
2. Get the underlying DbTransaction: `transaction.GetDbTransaction()`
3. Sub-contexts call `UseTransaction(dbTransaction)` to join
4. Parent context controls Commit/Rollback for ALL contexts
5. All contexts must share the same database connection (for relational DBs)

This is a RECOMMENDED pattern for coordinating multiple contexts in a single transaction.",
        pitfall: @"**Common Mistake:** Not understanding that sub-contexts need explicit UseTransaction() call.

This test demonstrates the CORRECT pattern:
1. Parent starts transaction
2. Parent makes changes
3. Sub-context joins using UseTransaction()
4. Sub-context can see parent's uncommitted changes
5. Sub-context can make its own changes
6. Parent commits - both contexts' changes are persisted

The test intentionally tries to query without committing to show the transaction boundary.",
        fix: @"**Solution - The Correct Pattern (requires relational database):**

```csharp
// This pattern works with SQL Server, PostgreSQL, SQLite, etc.
// NOT with In-Memory provider!

using var parentContext = new AppDbContext(options);
using var transaction = parentContext.Database.BeginTransaction();

// Parent context work
parentContext.Products.Add(product1);
parentContext.SaveChanges();

// Sub-context CORRECTLY joins the transaction
using (var subContext = new AppDbContext(options))
{
    subContext.Database.UseTransaction(transaction.GetDbTransaction());
    
    // Can see parent's uncommitted data
    var product = subContext.Products.Find(product1.Id);
    
    // Can make its own changes
    subContext.Orders.Add(order);
    subContext.SaveChanges();
}

// Commit ALL changes from both contexts
transaction.Commit();
```

**For In-Memory testing:** You cannot truly test this pattern. Use SQLite in-memory mode:
```csharp
var connection = new SqliteConnection(""DataSource=:memory:"");
connection.Open();
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connection)
    .Options;
```

This is the standard pattern for multi-context transactions.",
        order: 25
    )]
    public static void Test_Sharing_Transaction_Across_Contexts_Correctly()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Shared_Transaction_Correct")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        int productId;

        using (var parentContext = new AppDbContext(options))
        {
            using var transaction = parentContext.Database.BeginTransaction();
            
            // Parent context adds a product
            var product = new Product { Name = "Shared Transaction Product", Price = 100m, Stock = 10 };
            parentContext.Products.Add(product);
            parentContext.SaveChanges();
            productId = product.Id;
            
            // Sub-context CORRECTLY joins the transaction
            using (var subContext = new AppDbContext(options))
            {
                // THIS IS THE KEY: Share the transaction!
                // Note: GetDbTransaction() only works with relational databases
                try
                {
                    subContext.Database.UseTransaction(transaction.GetDbTransaction());
                }
                catch (InvalidOperationException)
                {
                    // Expected with In-Memory provider - it doesn't support GetDbTransaction()
                    // This test demonstrates the CONCEPT, but needs a real database to actually work
                    Assert.IsTrue(false,
                        "GetDbTransaction() requires a relational database provider. " +
                        "In-Memory doesn't support transaction sharing. Use SQLite or SQL Server for this pattern.");
                    return;
                }
                
                // Sub-context can see parent's uncommitted changes (with real DB)
                var foundProduct = subContext.Products.Find(productId);
                
                // Note: With In-Memory, this works differently (limitation)
                // But with real DB, foundProduct would be visible here
                
                // Sub-context adds an order
                var order = new Order 
                { 
                    CustomerName = "Transaction Customer", 
                    OrderDate = DateTime.Now,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = productId, Quantity = 2, PriceAtOrder = 100m }
                    }
                };
                subContext.Orders.Add(order);
                subContext.SaveChanges();
            }
            
            // BUG: Forgot to commit! Both contexts' changes will be rolled back
            // transaction.Commit(); // This is missing!
        }

        // Verify: Nothing should be persisted (no commit)
        using (var verifyContext = new AppDbContext(options))
        {
            var product = verifyContext.Products.Find(productId);
            var orders = verifyContext.Orders.ToList();
            
            // This will FAIL - changes from both contexts were rolled back
            Assert.IsNotNull(product, "Product should exist after transaction.Commit()");
            Assert.NotEmpty(orders, "Order should exist after transaction.Commit()");
        }
    }

    [Tutorial(
        title: "When Should You Use Sub-Contexts in Transactions?",
        category: "Transactions",
        concept: @"Sub-contexts in transactions are useful for several scenarios:

**Use Cases:**
1. **Repository Pattern**: Different repositories use different contexts for different aggregates
2. **Service Layer Coordination**: Multiple services need to participate in one transaction
3. **Separation of Concerns**: Different contexts handle different bounded contexts
4. **Long-Running Workflows**: Parent orchestrates, children do specific work
5. **Testing**: Isolate different operations while maintaining transaction control

**Anti-Pattern - When NOT to use sub-contexts:**
- Don't create sub-contexts for simple CRUD in a single bounded context
- Don't use it to 'organize' code when one context suffices
- Don't create hundreds of contexts (connection pool exhaustion)
- Performance: Each context has overhead

**Best Practice:**
Use ONE context per request/unit-of-work when possible. Use sub-contexts with shared transactions only when architectural patterns demand it (repositories, services).",
        pitfall: @"**Common Mistake:** Creating sub-contexts unnecessarily, leading to complexity without benefit.

This test demonstrates:
1. A scenario where sub-contexts make sense (repository pattern simulation)
2. Each 'repository' (represented by a context) handles its domain
3. Parent coordinates the transaction
4. But the test fails because the pattern isn't always necessary

The test intentionally creates unnecessary complexity to teach when to avoid this pattern.",
        fix: @"**Solution - Use sub-contexts judiciously:**

```csharp
// GOOD: Repository pattern with transaction coordination
public class OrderService
{
    public void CreateOrder(int productId, int quantity)
    {
        using var mainContext = new AppDbContext(options);
        using var transaction = mainContext.Database.BeginTransaction();
        
        // ProductRepository uses sub-context
        var product = GetProduct(productId, transaction);
        
        // OrderRepository uses another sub-context
        CreateOrderWithItem(productId, quantity, transaction);
        
        transaction.Commit();
    }
    
    private Product GetProduct(int id, IDbContextTransaction transaction)
    {
        using var productContext = new AppDbContext(options);
        productContext.Database.UseTransaction(transaction.GetDbTransaction());
        return productContext.Products.Find(id);
    }
}

// BETTER: Just use one context for simple scenarios!
using var context = new AppDbContext(options);
var product = context.Products.Find(productId);
context.Orders.Add(order);
context.SaveChanges(); // Simple and clear!
```",
        order: 26
    )]
    public static void Test_When_To_Use_SubContexts_In_Transactions()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_SubContext_UseCase")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        // Setup: Add a product first
        using (var setupContext = new AppDbContext(options))
        {
            setupContext.Products.Add(new Product { Name = "Repository Product", Price = 50m, Stock = 100 });
            setupContext.SaveChanges();
        }

        // Test: Simulate repository pattern (potentially over-engineered)
        using (var mainContext = new AppDbContext(options))
        {
            using var transaction = mainContext.Database.BeginTransaction();
            
            int productId;
            
            // 'ProductRepository' uses sub-context
            using (var productContext = new AppDbContext(options))
            {
                try
                {
                    productContext.Database.UseTransaction(transaction.GetDbTransaction());
                }
                catch (InvalidOperationException)
                {
                    // In-Memory doesn't support GetDbTransaction()
                    Assert.IsTrue(false,
                        "Transaction sharing across contexts requires a relational database. " +
                        "In-Memory provider doesn't support this. Use SQLite or SQL Server.");
                    return;
                }
                
                var product = productContext.Products.First(p => p.Name == "Repository Product");
                productId = product.Id;
                
                // Update stock
                product.Stock -= 5;
                productContext.SaveChanges();
            }
            
            // 'OrderRepository' uses another sub-context
            using (var orderContext = new AppDbContext(options))
            {
                orderContext.Database.UseTransaction(transaction.GetDbTransaction());
                
                var order = new Order
                {
                    CustomerName = "Multi-Context Customer",
                    OrderDate = DateTime.Now,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = productId, Quantity = 5, PriceAtOrder = 50m }
                    }
                };
                orderContext.Orders.Add(order);
                orderContext.SaveChanges();
            }
            
            // BUG: All this complexity, but we forget to commit!
            // transaction.Commit();
        }

        // Verify: Nothing persisted
        using (var verifyContext = new AppDbContext(options))
        {
            var product = verifyContext.Products.First(p => p.Name == "Repository Product");
            var orders = verifyContext.Orders.ToList();
            
            // These will FAIL - no commit means rollback
            Assert.AreEqual(95, product.Stock, "Stock should be reduced after commit");
            Assert.NotEmpty(orders, "Order should exist after commit");
        }
    }

    [Tutorial(
        title: "Nested Transactions Are Not Truly Nested (Savepoints)",
        category: "Transactions",
        concept: @"Most databases do NOT support true nested transactions. What happens:

**Attempting 'Nested' Transactions:**
- Calling BeginTransaction() while one is active throws an exception (SQL Server, PostgreSQL)
- OR it's silently ignored (some providers)
- OR it requires special savepoint support

**Savepoints (Advanced):**
Some databases support savepoints for 'nested' rollback:
- `transaction.CreateSavepoint(""name"")` 
- `transaction.RollbackToSavepoint(""name"")`
- Allows partial rollback within a transaction
- Not all providers support this

**Reality:** 
One transaction per connection. Sub-contexts share the SAME transaction, they don't create nested ones.

EF Core doesn't support true nested transactions - only shared transactions across contexts.",
        pitfall: @"**Common Mistake:** Thinking you can create nested transactions by calling BeginTransaction() multiple times.

This test demonstrates:
1. Parent context starts a transaction
2. Sub-context tries to start its OWN transaction
3. This either throws an exception or is ignored
4. You cannot have independent commit/rollback for 'nested' levels

The test FAILS to show this limitation.",
        fix: @"**Solution - Use shared transactions, not nested ones:**

```csharp
// WRONG: Trying to nest transactions
using var parent = new AppDbContext(options);
using var parentTxn = parent.Database.BeginTransaction();

using var child = new AppDbContext(options);
using var childTxn = child.Database.BeginTransaction(); // ERROR!

// RIGHT: Share the same transaction
using var parent = new AppDbContext(options);
using var transaction = parent.Database.BeginTransaction();

using var child = new AppDbContext(options);
child.Database.UseTransaction(transaction.GetDbTransaction()); // Correct!

// For partial rollback, use savepoints (if supported):
transaction.CreateSavepoint(""before_risky_operation"");
try 
{
    // risky operation
}
catch 
{
    transaction.RollbackToSavepoint(""before_risky_operation"");
}
transaction.Commit();
```",
        order: 27
    )]
    public static void Test_Nested_Transactions_Are_Not_Supported()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Nested_Transactions")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var parentContext = new AppDbContext(options);
        using var parentTransaction = parentContext.Database.BeginTransaction();
        
        // Parent makes a change
        parentContext.Products.Add(new Product { Name = "Parent Change", Price = 100m, Stock = 10 });
        parentContext.SaveChanges();
        
        // Try to create a 'nested' transaction
        try
        {
            using var childContext = new AppDbContext(options);
            
            // BUG: Trying to start a NEW transaction (not share the existing one)
            // With real databases, this often throws InvalidOperationException
            // With In-Memory, it might be ignored
            using var childTransaction = childContext.Database.BeginTransaction();
            
            childContext.Products.Add(new Product { Name = "Child Change", Price = 50m, Stock = 5 });
            childContext.SaveChanges();
            
            // Trying to independently commit the 'child' transaction
            childTransaction.Commit();
            
            // This pattern is wrong - you can't nest transactions this way!
            Assert.IsTrue(false, 
                "Nested transactions are not supported! " +
                "Use shared transactions with UseTransaction() or savepoints for partial rollback.");
        }
        catch (InvalidOperationException)
        {
            // Expected with real databases
            // Test 'fails' to teach the lesson
            Assert.IsTrue(false, 
                "Attempting to nest transactions throws an exception. " +
                "Use transaction sharing with UseTransaction() instead.");
        }
        finally
        {
            parentTransaction.Rollback();
        }
    }
}
