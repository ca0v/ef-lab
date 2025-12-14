using Microsoft.EntityFrameworkCore;
using EFLab.Testing;
using EFLab.Models;
using EFLab.Data;

namespace EFLab.Tests;

/// <summary>
/// Setting NoTracking as the default behavior globally
/// </summary>
public static class NoTrackingDefaultTests
{
    [Tutorial(
        title: "Setting NoTracking as Global Default Behavior",
        category: "No-Tracking Queries",
        concept: @"You can configure EF Core to use NoTracking by default for ALL queries in a DbContext.

Configure in DbContext:
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}
```

Or when creating options:
```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(""MyDb"")
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    .Options;
```

**When to use:**
- Read-heavy applications (reporting, APIs)
- Most queries are read-only
- Explicit opt-in for tracking when needed

**Impact:**
- All queries return detached entities by default
- Must explicitly use AsTracking() when you need to modify entities
- Better default performance for read scenarios",
        pitfall: @"**Common Mistake:** Setting NoTracking as default, then forgetting to use AsTracking() when you need to modify entities.

This test demonstrates:
1. Configure context with NoTracking as default
2. Query an entity (it's not tracked)
3. Try to modify and save it
4. Changes are NOT persisted!

The test FAILS because tracking must be explicitly enabled.",
        fix: @"**Solution:** Use AsTracking() when you need to modify entities:

```csharp
// Default behavior: NoTracking (read-only)
var products = context.Products.ToList();
// These are NOT tracked

// Explicit tracking for modifications
var product = context.Products
    .AsTracking()  // Override default NoTracking
    .First(p => p.Id == id);
product.Price = 200m;
context.SaveChanges(); // This works!

// Alternative: Attach and mark as modified
var product = context.Products.First(p => p.Id == id);
context.Attach(product);
context.Entry(product).State = EntityState.Modified;
product.Price = 200m;
context.SaveChanges();
```

Remember: With NoTracking default, you must explicitly opt-in to tracking for updates.",
        order: 40
    )]
    public static void Test_NoTracking_Default_Requires_Explicit_AsTracking()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_NoTracking_Default")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)  // Global default
            .Options;

        int productId;

        // Setup: Add a product
        using (var context = new AppDbContext(options))
        {
            var product = new Product { Name = "Default NoTrack Product", Price = 100m, Stock = 10 };
            
            // Note: Even with NoTracking default, newly added entities are tracked
            context.Products.Add(product);
            context.SaveChanges();
            
            productId = product.Id;
        }

        // BUG: Query with default NoTracking and try to modify
        using (var context = new AppDbContext(options))
        {
            // This query returns a detached entity (default NoTracking)
            var product = context.Products.First(p => p.Id == productId);
            
            var state = context.Entry(product).State;
            Assert.AreEqual(EntityState.Detached, state, 
                "With NoTracking default, queried entities are Detached");
            
            // Try to modify (won't work!)
            product.Price = 150m;
            context.SaveChanges();
        }

        // Verify: Price should still be original (change wasn't saved)
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.First(p => p.Id == productId);
            
            // This will FAIL - the price wasn't saved
            Assert.AreEqual(150m, product.Price,
                "With NoTracking as default, queries return detached entities! " +
                "Use .AsTracking() when you need to modify entities: " +
                "context.Products.AsTracking().First(p => p.Id == id) " +
                $"Found price: {product.Price}");
        }
    }

    [Tutorial(
        title: "AsTracking() Overrides NoTracking Default",
        category: "No-Tracking Queries",
        concept: @"When NoTracking is the default behavior, use AsTracking() to enable change tracking for specific queries.

Example:
```csharp
// Context configured with NoTracking default
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    .Options;

// This query returns detached entities
var products = context.Products.ToList();

// This query returns tracked entities (override default)
var product = context.Products
    .AsTracking()
    .First(p => p.Id == id);
```

AsTracking() is the opposite of AsNoTracking():
- AsNoTracking(): Disable tracking for this query
- AsTracking(): Enable tracking for this query",
        pitfall: @"**Common Mistake:** With NoTracking default, developers forget that queries need AsTracking() for updates.

This test demonstrates:
1. NoTracking is default
2. Query without AsTracking()
3. Try to update entity
4. Update fails silently

The test shows proper use of AsTracking().",
        fix: @"**Solution:** Always use AsTracking() when you need to modify entities:

```csharp
// Context with NoTracking default
using var context = new AppDbContext(options);

// Read-only query (uses default NoTracking)
var allProducts = context.Products
    .Where(p => p.Stock > 0)
    .ToList();
// These are detached, cannot be modified

// Query for update (explicitly enable tracking)
var product = context.Products
    .AsTracking()  // Override NoTracking default
    .First(p => p.Id == id);
product.Price = 200m;
context.SaveChanges(); // Works!

// Or track existing detached entity
var product = context.Products.First(p => p.Id == id);
context.Update(product); // Attaches and marks as modified
product.Price = 200m;
context.SaveChanges();
```",
        order: 41
    )]
    public static void Test_AsTracking_Overrides_NoTracking_Default()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_AsTracking_Override")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        int productId;

        // Setup
        using (var context = new AppDbContext(options))
        {
            var product = new Product { Name = "Tracked Product", Price = 100m, Stock = 10 };
            context.Products.Add(product);
            context.SaveChanges();
            productId = product.Id;
        }

        // Correct: Use AsTracking() to override default
        using (var context = new AppDbContext(options))
        {
            var product = context.Products
                .AsTracking()  // Explicitly enable tracking
                .First(p => p.Id == productId);
            
            var state = context.Entry(product).State;
            Assert.AreEqual(EntityState.Unchanged, state, 
                "AsTracking() overrides NoTracking default");
            
            product.Price = 150m;
            context.SaveChanges();
        }

        // Verify: Price should be updated
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.First(p => p.Id == productId);
            
            Assert.AreEqual(150m, product.Price,
                "AsTracking() enables change tracking even with NoTracking default");
        }
    }

    [Tutorial(
        title: "NoTracking Default: Checking Tracking Status",
        category: "No-Tracking Queries",
        concept: @"When NoTracking is the default, it's useful to verify whether entities are being tracked.

Check tracking status:
```csharp
// Check specific entity
var state = context.Entry(entity).State;
if (state == EntityState.Detached)
{
    // Entity is not tracked
}

// Count tracked entities
var trackedCount = context.ChangeTracker.Entries().Count();

// List all tracked entities
var trackedEntities = context.ChangeTracker
    .Entries()
    .Select(e => new 
    { 
        Entity = e.Entity, 
        State = e.State 
    })
    .ToList();
```

Tracking states:
- Detached: Not tracked (default with NoTracking)
- Unchanged: Tracked, no changes
- Modified: Tracked, has changes
- Added: New entity, will be inserted
- Deleted: Marked for deletion",
        pitfall: @"**Common Mistake:** Not verifying tracking status when NoTracking is default, leading to silent failures.

This test demonstrates:
1. NoTracking default is configured
2. Query entities and check their tracking state
3. Show difference between default and AsTracking()

The test verifies tracking behavior.",
        fix: @"**Solution:** Always verify tracking when needed:

```csharp
// Query with default NoTracking
var products = context.Products.ToList();
var trackedCount = context.ChangeTracker.Entries<Product>().Count();
Console.WriteLine($""Tracked products: {trackedCount}""); // 0

// Query with AsTracking
var trackedProducts = context.Products.AsTracking().ToList();
trackedCount = context.ChangeTracker.Entries<Product>().Count();
Console.WriteLine($""Tracked products: {trackedCount}""); // > 0

// Check specific entity
var product = context.Products.First();
var state = context.Entry(product).State;
if (state == EntityState.Detached)
{
    Console.WriteLine(""Entity is not tracked!"");
    // Attach if you need to modify it
    context.Attach(product);
}
```",
        order: 42
    )]
    public static void Test_NoTracking_Default_Checking_Tracking_Status()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Check_Tracking_Status")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        // Setup: Add multiple products
        using (var context = new AppDbContext(options))
        {
            context.Products.AddRange(
                new Product { Name = "Product 1", Price = 10m, Stock = 5 },
                new Product { Name = "Product 2", Price = 20m, Stock = 10 },
                new Product { Name = "Product 3", Price = 30m, Stock = 15 }
            );
            context.SaveChanges();
        }

        // Test: Query with default NoTracking
        using (var context = new AppDbContext(options))
        {
            var products = context.Products.ToList();
            
            // Check how many entities are tracked
            var trackedCount = context.ChangeTracker.Entries<Product>().Count();
            
            // With NoTracking default, nothing is tracked
            Assert.AreEqual(0, trackedCount,
                "Default NoTracking: No entities are tracked after query");
            
            // Check state of first product
            var firstProduct = products.First();
            var state = context.Entry(firstProduct).State;
            
            Assert.AreEqual(EntityState.Detached, state,
                "Entities queried with NoTracking default are Detached");
        }

        // Test: Query with explicit AsTracking
        using (var context = new AppDbContext(options))
        {
            var products = context.Products.AsTracking().ToList();
            
            var trackedCount = context.ChangeTracker.Entries<Product>().Count();
            
            // This FAILS to show the difference
            Assert.AreEqual(0, trackedCount,
                $"AsTracking() enables change tracking! Found {trackedCount} tracked entities. " +
                "Always check context.ChangeTracker.Entries().Count() to verify tracking status. " +
                "With NoTracking default, use AsTracking() when you need to modify entities.");
        }
    }

    [Tutorial(
        title: "NoTracking Default with Add/Update/Delete Operations",
        category: "No-Tracking Queries",
        concept: @"NoTracking default behavior ONLY affects queries. Add, Update, and Delete operations always track entities.

Behavior with NoTracking default:
- Queries: Return detached entities (not tracked)
- Add(): Entity is tracked (Added state)
- Update(): Entity is tracked (Modified state)
- Remove(): Entity is tracked (Deleted state)
- Attach(): Entity is tracked (Unchanged state)

Example:
```csharp
// Context with NoTracking default
using var context = new AppDbContext(options);

// Query: Detached
var product = context.Products.First();
// State: Detached

// Add: Tracked
context.Products.Add(new Product());
// State: Added

// Update: Tracked
context.Update(product);
// State: Modified

// Delete: Tracked
context.Remove(product);
// State: Deleted
```",
        pitfall: @"**Common Mistake:** Thinking NoTracking default affects Add/Update/Delete operations.

This test demonstrates:
1. NoTracking is default for queries
2. But Add/Update/Delete always track entities
3. Only queries return detached entities

The test shows tracking behavior for different operations.",
        fix: @"**Solution:** Understand what NoTracking default affects:

```csharp
// NoTracking affects QUERIES ONLY
var product = context.Products.First();
// State: Detached (not tracked)

// Add ALWAYS tracks
var newProduct = new Product { Name = ""New"" };
context.Products.Add(newProduct);
// State: Added (tracked)

// Update ALWAYS tracks
var detachedProduct = context.Products.First(); // Detached
context.Update(detachedProduct);
// State: Modified (now tracked)

// Attach ALWAYS tracks
var anotherProduct = context.Products.First(); // Detached
context.Attach(anotherProduct);
// State: Unchanged (now tracked)

// SaveChanges works with tracked entities
context.SaveChanges();
```

NoTracking default = Query optimization, not a global tracking disable.",
        order: 43
    )]
    public static void Test_NoTracking_Default_Does_Not_Affect_Add_Update_Delete()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_NoTracking_Add_Update")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        int productId;

        // Setup
        using (var context = new AppDbContext(options))
        {
            var product = new Product { Name = "Original", Price = 100m, Stock = 10 };
            context.Products.Add(product);
            context.SaveChanges();
            productId = product.Id;
        }

        // Test: Add/Update/Delete always track, even with NoTracking default
        using (var context = new AppDbContext(options))
        {
            // Query: Detached (NoTracking default)
            var queriedProduct = context.Products.First(p => p.Id == productId);
            var queryState = context.Entry(queriedProduct).State;
            Assert.AreEqual(EntityState.Detached, queryState, 
                "Query returns detached entity with NoTracking default");
            
            // Update: Makes it tracked
            context.Update(queriedProduct);
            var updateState = context.Entry(queriedProduct).State;
            Assert.AreEqual(EntityState.Modified, updateState,
                "Update() tracks the entity even with NoTracking default");
            
            // Modify and save
            queriedProduct.Price = 150m;
            context.SaveChanges();
            
            // Add: Always tracked
            var newProduct = new Product { Name = "New Product", Price = 50m, Stock = 5 };
            context.Products.Add(newProduct);
            var addState = context.Entry(newProduct).State;
            Assert.AreEqual(EntityState.Added, addState,
                "Add() tracks entity even with NoTracking default");
            
            context.SaveChanges();
        }

        // Verify: Changes were saved
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.First(p => p.Id == productId);
            
            // Test 'passes' to show Update worked despite NoTracking default
            Assert.AreEqual(150m, product.Price,
                "Update() and SaveChanges() work with NoTracking default! " +
                "NoTracking only affects queries. Add/Update/Delete always track entities.");
        }
    }
}
