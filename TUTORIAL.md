# Entity Framework Core Tutorial

A failing-first approach to learning EF Core. Each test intentionally fails to teach you common pitfalls and how to fix them.

# CoreConcepts Tests

## SaveChanges Is Required for Persistence

**Category:** Core Concepts

### Concept
Entity Framework Core uses a Unit of Work pattern through DbContext. Changes to tracked entities are kept in memory until SaveChanges() is called. This batches database operations for efficiency.

Key points:
- Add(), Update(), Remove() only modify the change tracker in memory
- No database operations occur until SaveChanges()
- SaveChanges() generates and executes SQL statements as a batch
- Returns the number of entities affected

### The Pitfall
**Common Mistake:** Developers forget to call SaveChanges() and expect data to be persisted automatically.

This test intentionally:
1. Creates a new Product
2. Adds it to the DbContext
3. Does NOT call SaveChanges()
4. Expects to find the product in a new query

Result: The test FAILS because the product was never saved to the database.

### The Fix
**Solution:** Always call SaveChanges() after modifying entities.

```csharp
context.Products.Add(product);
await context.SaveChangesAsync(); // or context.SaveChanges()
```

The test will pass once SaveChanges() is added after the Add() operation.


---

## Entity State Tracking

**Category:** Core Concepts

### Concept
EF Core tracks the state of each entity in the DbContext. Entity states:

- **Detached**: Not tracked by the context
- **Added**: New entity, will be inserted on SaveChanges()
- **Unchanged**: Tracked, no modifications detected
- **Modified**: Tracked, changes detected, will be updated on SaveChanges()
- **Deleted**: Marked for deletion, will be deleted on SaveChanges()

You can check state with: `context.Entry(entity).State`

### The Pitfall
**Common Mistake:** Assuming entities are automatically tracked or not understanding when state changes.

This test demonstrates:
1. Creating an entity (Detached state)
2. Adding it to context (Added state)
3. After SaveChanges (Unchanged state)
4. Modifying a property (Modified state)

The test intentionally checks the wrong state at each step.

### The Fix
**Solution:** Understand the entity lifecycle:

```csharp
var product = new Product(); // Detached
context.Products.Add(product); // Added
context.SaveChanges(); // Now Unchanged
product.Price = 100; // Now Modified
context.SaveChanges(); // Back to Unchanged
```


---

## Change Tracking Detects Property Changes

**Category:** Core Concepts

### Concept
EF Core's change tracker automatically detects when properties of tracked entities change. It does this by:

1. Taking a snapshot of property values when an entity is first tracked
2. Comparing current values to the snapshot when SaveChanges() is called
3. Generating UPDATE statements only for modified properties

This is called 'snapshot change tracking' and is the default for most entity types.

### The Pitfall
**Common Mistake:** Modifying an entity and expecting immediate database updates without SaveChanges().

This test:
1. Loads a product from the database
2. Modifies its price property
3. Checks if the change is detected (it is)
4. But expects the database to be updated immediately (it's not)

The test FAILS because changes are only in memory until SaveChanges().

### The Fix
**Solution:** Call SaveChanges() to persist modifications:

```csharp
var product = context.Products.First();
product.Price = 199.99m;
// Change is tracked but NOT in database yet
context.SaveChanges(); // NOW it's persisted
```


---

## Delete Operations Require SaveChanges

**Category:** Core Concepts

### Concept
Deleting entities in EF Core follows the same Unit of Work pattern. The Remove() method marks an entity for deletion, but doesn't execute the DELETE statement immediately.

Key points:
- Remove() marks entity as Deleted in the change tracker
- RemoveRange() can delete multiple entities at once
- The database DELETE only happens on SaveChanges()
- The entity remains in memory (marked as Deleted) until SaveChanges()
- After SaveChanges(), the entity state becomes Detached

### The Pitfall
**Common Mistake:** Calling Remove() and expecting the entity to be immediately deleted from the database.

This test:
1. Adds a product and saves it
2. Calls Remove() on the product
3. Checks the entity state (correctly shows Deleted)
4. Queries in a new context expecting the product to be gone (it's not!)

The test FAILS because SaveChanges() was never called after Remove().

### The Fix
**Solution:** Call SaveChanges() after Remove() to persist the deletion:

```csharp
var product = context.Products.First();
context.Products.Remove(product);
context.SaveChanges(); // NOW it's deleted from database
```


---

## Querying One-to-Many Relationships with Include

**Category:** Core Concepts - Relationships

### Concept
EF Core uses lazy loading by default, but explicitly loading related data requires the Include() method. This performs a JOIN to load parent and child entities together.

Key points:
- Include() eagerly loads navigation properties
- Without Include(), navigation properties are null (unless lazy loading is enabled)
- Can chain multiple Include() calls for different relationships
- Use ThenInclude() for nested relationships (grandchildren)
- This generates a SQL JOIN statement

### The Pitfall
**Common Mistake:** Accessing navigation properties without using Include() and getting null or empty collections.

This test:
1. Creates an Order with OrderItems
2. Saves everything correctly
3. Queries the Order WITHOUT Include()
4. Expects the Items collection to be populated (it's not!)

The test FAILS because related data must be explicitly loaded with Include().

### The Fix
**Solution:** Use Include() to load related entities:

```csharp
var order = context.Orders
    .Include(o => o.Items)  // Load the Items collection
    .First(o => o.CustomerName == "John");

// Now order.Items is populated
```


---

## Adding Related Entities (One-to-Many)

**Category:** Core Concepts - Relationships

### Concept
When adding entities with relationships, EF Core can track both parent and child entities if they're properly connected. You can add the parent with navigation properties populated, and EF will handle the foreign keys.

Key points:
- Adding a parent entity with populated navigation properties adds all related children
- EF Core automatically sets foreign key values
- The order of operations matters less when using navigation properties
- All entities must be in the same context
- Still requires SaveChanges() to persist everything

### The Pitfall
**Common Mistake:** Adding related entities but forgetting SaveChanges(), or adding them to different contexts.

This test:
1. Creates an Order with OrderItems through navigation properties
2. Adds the Order to the context
3. Does NOT call SaveChanges()
4. Tries to query the order and items in a new context

The test FAILS because nothing was persisted to the database.

### The Fix
**Solution:** Add parent with children, then call SaveChanges():

```csharp
var order = new Order
{
    CustomerName = "Jane",
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = 1, Quantity = 3 }
    }
};
context.Orders.Add(order);
context.SaveChanges(); // Persists both order and items
```


---

## Deleting Parent Deletes Children (Cascade Delete)

**Category:** Core Concepts - Relationships

### Concept
By default, EF Core configures cascade delete for required relationships. When you delete a parent entity, all dependent child entities are automatically deleted.

Key points:
- Cascade delete is the default for required (non-nullable foreign key) relationships
- Children are marked as Deleted when parent is deleted
- All deletes happen in one transaction on SaveChanges()
- You can configure different cascade behaviors (Restrict, SetNull, etc.)
- The children don't need to be loaded for cascade delete to work

### The Pitfall
**Common Mistake:** Deleting a parent entity but forgetting SaveChanges(), or not understanding cascade behavior.

This test:
1. Creates an Order with OrderItems
2. Saves everything properly
3. Loads and deletes the Order
4. Does NOT call SaveChanges()
5. Expects both order and items to be gone

The test FAILS because SaveChanges() was never called after the delete.

### The Fix
**Solution:** Call SaveChanges() after deleting the parent:

```csharp
var order = context.Orders.Find(orderId);
context.Orders.Remove(order);
context.SaveChanges(); // Deletes order AND all OrderItems
```

Note: You don't need to load Items to delete them - cascade handles it.


---

# InMemoryTesting Tests

## In-Memory Database Does Not Enforce Relational Constraints

**Category:** In-Memory Testing

### Concept
The EF Core In-Memory provider is great for fast tests, but it's NOT a relational database. It doesn't enforce many constraints that real databases do.

Key limitations:
- No foreign key constraints enforcement
- No unique constraints (other than primary keys)
- No check constraints
- No database-side computed columns
- No stored procedures or triggers
- Simpler query translation (some LINQ queries that work in-memory fail on real DBs)

The In-Memory provider is a fake that stores data in memory collections, not a real database engine.

### The Pitfall
**Common Mistake:** Assuming tests that pass with In-Memory will work the same way with SQL Server, PostgreSQL, etc.

This test demonstrates:
1. Creating an OrderItem with an invalid ProductId (doesn't exist)
2. Saving it successfully with In-Memory database
3. This would FAIL with a real database (foreign key violation)

The test 'passes' with In-Memory but reveals the pitfall - your app could have bugs that only appear in production!

### The Fix
**Solution:** Understand In-Memory limitations and supplement with integration tests:

```csharp
// For unit tests: In-Memory is fast and good enough
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase("test")
    .Options;

// For integration tests: Use real database or SQLite
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("DataSource=:memory:")
    .Options;
```

SQLite in-memory mode enforces constraints but is still fast for testing.


---

## In-Memory Database Does Not Persist Between Context Instances

**Category:** In-Memory Testing

### Concept
Each In-Memory database instance is identified by a database name string. The data persists in memory as long as:

1. At least one context using that database name exists, OR
2. You explicitly maintain a reference to keep it alive

Once all contexts are disposed and no root references exist, the data is garbage collected.

This is different from SQLite :memory: which persists for the connection lifetime.

### The Pitfall
**Common Mistake:** Expecting In-Memory data to automatically persist across test methods or test classes without careful database name management.

This test demonstrates:
1. Creating a database with a unique name
2. Adding data and disposing the context
3. Creating a new context with the SAME database name
4. The data should still be there... but is it?

The test structure shows how database name management affects data persistence.

### The Fix
**Solution:** Share database names carefully and manage context lifecycles:

```csharp
// Option 1: Use unique DB names per test (isolated)
UseInMemoryDatabase($"Test_{Guid.NewGuid()}")

// Option 2: Share DB name across operations (persisted)
const string sharedDbName = "SharedTestDb";
UseInMemoryDatabase(sharedDbName)

// Option 3: Keep root context alive
var root = new AppDbContext(options);
// Data persists while root exists
```


---

## In-Memory Database Does Not Support Raw SQL Queries

**Category:** In-Memory Testing

### Concept
The In-Memory provider does not support executing raw SQL queries because there's no SQL engine. Methods like:

- FromSqlRaw()
- FromSqlInterpolated()
- ExecuteSqlRaw()
- ExecuteSqlInterpolated()

These will throw NotSupportedException with In-Memory provider.

This is a significant limitation if your application uses stored procedures, views, or optimized SQL queries.

### The Pitfall
**Common Mistake:** Writing tests with In-Memory that never exercise your raw SQL code paths.

This test:
1. Attempts to use FromSqlRaw() to query products
2. With In-Memory, this throws NotSupportedException
3. Your tests pass without raw SQL, but production code uses it

The test FAILS to demonstrate that In-Memory can't test raw SQL scenarios.

### The Fix
**Solution:** Use SQLite or real database for tests involving raw SQL:

```csharp
// Won't work with In-Memory:
var products = context.Products
    .FromSqlRaw("SELECT * FROM Products WHERE Price > {0}", 100)
    .ToList();

// For testing raw SQL, use SQLite:
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("DataSource=:memory:")
    .Options;

using var connection = new SqliteConnection("DataSource=:memory:");
connection.Open();
// Now raw SQL works!
```


---

## In-Memory Database Query Translation Differs from Real Databases

**Category:** In-Memory Testing

### Concept
The In-Memory provider translates LINQ queries to in-memory operations, not SQL. Some queries that work in-memory will fail with real databases (and vice versa).

Differences include:
- Case sensitivity in string comparisons
- Client-side evaluation is more permissive
- Some SQL-specific functions don't exist in-memory
- Date/time operations may differ
- Collation and culture differences

Tests passing with In-Memory don't guarantee the LINQ will translate to valid SQL.

### The Pitfall
**Common Mistake:** Using client-evaluated expressions that work in-memory but fail in production.

This test demonstrates:
1. A LINQ query using client-side method call
2. It works with In-Memory (evaluates in .NET)
3. With SQL Server, it might fail or behave differently

The test intentionally uses a pattern that highlights this difference.

### The Fix
**Solution:** Be aware of query translation differences:

```csharp
// Might work in-memory but not in SQL:
.Where(p => IsExpensive(p.Price)) // Custom method

// Better - translatable to SQL:
.Where(p => p.Price > 100)

// Test with warnings enabled:
optionsBuilder
    .UseInMemoryDatabase("test")
    .ConfigureWarnings(w => w.Throw(
        InMemoryEventId.TransactionIgnoredWarning));
```

Consider using EF.Functions for database-specific operations.


---

# Transaction Tests

## Transactions Require Explicit Commit

**Category:** Transactions

### Concept
EF Core supports explicit transactions through BeginTransaction(). This gives you control over when changes are committed or rolled back.

Key points:
- BeginTransaction() starts a transaction
- SaveChanges() writes to the database but doesn't commit the transaction
- Commit() makes changes permanent
- Rollback() or Dispose() without Commit() undoes all changes
- SaveChanges() within a transaction is atomic with Commit()

Without an explicit transaction, each SaveChanges() auto-commits.

### The Pitfall
**Common Mistake:** Starting a transaction, calling SaveChanges(), but forgetting to Commit().

This test demonstrates:
1. BeginTransaction() starts a transaction
2. SaveChanges() executes SQL but doesn't commit
3. Transaction is disposed without Commit()
4. All changes are rolled back automatically

The test FAILS because the product was never committed to the database.

### The Fix
**Solution:** Always call Commit() on transactions:

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
```


---

## Rollback Undoes All Changes in Transaction

**Category:** Transactions

### Concept
Transactions provide atomicity - either all changes succeed or all fail together. Rollback() explicitly undoes all changes made within the transaction.

Key points:
- Rollback() undoes all operations since BeginTransaction()
- Multiple SaveChanges() calls within a transaction are all rolled back
- Dispose() without Commit() implicitly rolls back
- Rollback is useful for error handling
- Changes are only visible within the transaction until committed

### The Pitfall
**Common Mistake:** Not understanding that Rollback() undoes ALL changes, not just the last SaveChanges().

This test demonstrates:
1. Add multiple products in a transaction
2. SaveChanges() after each add
3. Explicitly call Rollback()
4. All products are gone, not just the last one

The test intentionally expects one product to remain (it won't).

### The Fix
**Solution:** Understand rollback affects the entire transaction:

```csharp
using var transaction = context.Database.BeginTransaction();

context.Products.Add(product1);
context.SaveChanges(); // Executed but not committed

context.Products.Add(product2);
context.SaveChanges(); // Executed but not committed

transaction.Rollback(); // Both products are undone!
// Neither product exists in the database
```


---

## Nested Transactions with Sub-Contexts Share Parent Transaction

**Category:** Transactions

### Concept
When you create a sub-context (child DbContext) and want it to participate in a parent's transaction, you must explicitly use UseTransaction() to share the transaction.

Key points:
- Each DbContext has its own connection by default
- Sub-contexts don't automatically share parent transactions
- Use context.Database.UseTransaction() to share a transaction
- Both contexts must use the same underlying database connection
- The parent transaction controls commit/rollback for all participating contexts

### The Pitfall
**Common Mistake:** Creating a sub-context and assuming it automatically participates in the parent's transaction.

This test demonstrates:
1. Parent context starts a transaction
2. Parent adds a product and saves
3. Sub-context is created (without UseTransaction)
4. Sub-context tries to query the product
5. Product is not visible because sub-context is in a different transaction!

The test FAILS because the sub-context can't see uncommitted changes from the parent.

### The Fix
**Solution:** Share the transaction explicitly:

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
```


---

## Partial Commits Don't Exist - Transaction is All or Nothing

**Category:** Transactions

### Concept
A transaction is atomic - you cannot partially commit some changes and rollback others. Once you commit, ALL changes in the transaction are permanent.

Key points:
- Commit() applies all SaveChanges() calls in the transaction
- You cannot selectively commit individual operations
- To have different commit boundaries, use separate transactions
- Nested transactions are not truly nested - they share the same underlying transaction
- Savepoints can provide partial rollback in some databases (advanced topic)

### The Pitfall
**Common Mistake:** Thinking you can commit some changes and rollback others within the same transaction.

This test demonstrates:
1. Add multiple products in a transaction
2. Call SaveChanges() after each
3. Try to 'partially commit' by calling Commit() in the middle
4. Then try to rollback remaining changes

In reality, Commit() ends the transaction - you can't continue it!

### The Fix
**Solution:** Use separate transactions for independent commit boundaries:

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
```


---

## Multiple Contexts with Shared Transaction Must Use Same Connection

**Category:** Transactions

### Concept
When multiple DbContext instances need to share a transaction, they must share the same underlying database connection. In-Memory provider handles this differently than relational databases.

Key points:
- Relational databases: Use the same DbConnection for all contexts
- In-Memory: Transactions are largely ignored (limitation!)
- For real databases, create connection first, then contexts
- All contexts must call UseTransaction() with the shared transaction
- The connection must remain open for the duration of the transaction

### The Pitfall
**Common Mistake:** With In-Memory provider, transaction behavior differs from real databases.

This test demonstrates:
1. In-Memory provider doesn't truly support transactions across contexts
2. Transaction.Commit() and Rollback() are essentially no-ops
3. Changes are immediately visible regardless of transaction state

The test FAILS to highlight this In-Memory limitation.

### The Fix
**Solution:** Use SQLite or real database for transaction testing:

```csharp
// For real database transaction testing:
using var connection = new SqliteConnection("DataSource=:memory:");
connection.Open();

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connection)
    .Options;

using var context1 = new AppDbContext(options);
using var transaction = context1.Database.BeginTransaction();

using var context2 = new AppDbContext(options);
context2.Database.UseTransaction(transaction.GetDbTransaction());

// Now both contexts share the same real transaction
```


---

## Sharing Transactions Across Multiple Contexts (The RIGHT Way)

**Category:** Transactions

### Concept
YES, you can share a transaction across multiple DbContext instances! This is a common pattern when using:
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

This is a RECOMMENDED pattern for coordinating multiple contexts in a single transaction.

### The Pitfall
**Common Mistake:** Not understanding that sub-contexts need explicit UseTransaction() call.

This test demonstrates the CORRECT pattern:
1. Parent starts transaction
2. Parent makes changes
3. Sub-context joins using UseTransaction()
4. Sub-context can see parent's uncommitted changes
5. Sub-context can make its own changes
6. Parent commits - both contexts' changes are persisted

The test intentionally tries to query without committing to show the transaction boundary.

### The Fix
**Solution - The Correct Pattern (requires relational database):**

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
var connection = new SqliteConnection("DataSource=:memory:");
connection.Open();
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connection)
    .Options;
```

This is the standard pattern for multi-context transactions.


---

## When Should You Use Sub-Contexts in Transactions?

**Category:** Transactions

### Concept
Sub-contexts in transactions are useful for several scenarios:

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
Use ONE context per request/unit-of-work when possible. Use sub-contexts with shared transactions only when architectural patterns demand it (repositories, services).

### The Pitfall
**Common Mistake:** Creating sub-contexts unnecessarily, leading to complexity without benefit.

This test demonstrates:
1. A scenario where sub-contexts make sense (repository pattern simulation)
2. Each 'repository' (represented by a context) handles its domain
3. Parent coordinates the transaction
4. But the test fails because the pattern isn't always necessary

The test intentionally creates unnecessary complexity to teach when to avoid this pattern.

### The Fix
**Solution - Use sub-contexts judiciously:**

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
```


---

## Nested Transactions Are Not Truly Nested (Savepoints)

**Category:** Transactions

### Concept
Most databases do NOT support true nested transactions. What happens:

**Attempting 'Nested' Transactions:**
- Calling BeginTransaction() while one is active throws an exception (SQL Server, PostgreSQL)
- OR it's silently ignored (some providers)
- OR it requires special savepoint support

**Savepoints (Advanced):**
Some databases support savepoints for 'nested' rollback:
- `transaction.CreateSavepoint("name")` 
- `transaction.RollbackToSavepoint("name")`
- Allows partial rollback within a transaction
- Not all providers support this

**Reality:** 
One transaction per connection. Sub-contexts share the SAME transaction, they don't create nested ones.

EF Core doesn't support true nested transactions - only shared transactions across contexts.

### The Pitfall
**Common Mistake:** Thinking you can create nested transactions by calling BeginTransaction() multiple times.

This test demonstrates:
1. Parent context starts a transaction
2. Sub-context tries to start its OWN transaction
3. This either throws an exception or is ignored
4. You cannot have independent commit/rollback for 'nested' levels

The test FAILS to show this limitation.

### The Fix
**Solution - Use shared transactions, not nested ones:**

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
transaction.CreateSavepoint("before_risky_operation");
try 
{
    // risky operation
}
catch 
{
    transaction.RollbackToSavepoint("before_risky_operation");
}
transaction.Commit();
```


---

