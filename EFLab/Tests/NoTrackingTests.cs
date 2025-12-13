using Microsoft.EntityFrameworkCore;
using EFLab.Testing;
using EFLab.Models;
using EFLab.Data;

namespace EFLab.Tests;

/// <summary>
/// No-Tracking Queries: AsNoTracking, performance, and tracking conflicts
/// </summary>
public static class NoTrackingTests
{
    [Tutorial(
        title: "No-Tracking Queries Cannot Be Modified and Saved",
        category: "No-Tracking Queries",
        concept: @"AsNoTracking() tells EF Core to NOT track entities in the change tracker. This improves query performance when you only need read-only data.

Key points:
- AsNoTracking() entities are not tracked by the DbContext
- Modifying properties has no effect on SaveChanges()
- Better performance (no snapshot creation, no change detection)
- Useful for read-only scenarios (reports, displays, DTOs)
- Entity state is Detached (not tracked)

Without AsNoTracking(), all queried entities are tracked by default.",
        pitfall: @"**Common Mistake:** Using AsNoTracking() and then trying to modify and save the entity.

This test demonstrates:
1. Query a product using AsNoTracking()
2. Modify the product's price
3. Call SaveChanges()
4. The change is NOT persisted (entity not tracked!)

The test FAILS because the modified price was never saved to the database.",
        fix: @"**Solution:** Don't use AsNoTracking() if you need to modify entities:

```csharp
// For read-only: AsNoTracking (better performance)
var product = context.Products
    .AsNoTracking()
    .First(p => p.Id == id);
// Cannot save changes to this entity

// For modifications: Regular tracking query
var product = context.Products
    .First(p => p.Id == id);
product.Price = 200m;
context.SaveChanges(); // This works!
```

Or attach the entity if you must modify a no-tracking entity:
```csharp
var product = context.Products.AsNoTracking().First();
context.Attach(product); // Now it's tracked
product.Price = 200m;
context.SaveChanges();
```",
        order: 30
    )]
    public static void Test_NoTracking_Entities_Cannot_Be_Saved()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_NoTracking_Cannot_Save")
            .Options;

        // Setup: Add a product
        using (var context = new AppDbContext(options))
        {
            context.Products.Add(new Product { Name = "No-Track Product", Price = 100m, Stock = 10 });
            context.SaveChanges();
        }

        // Test: Query with AsNoTracking and try to modify
        using (var context = new AppDbContext(options))
        {
            var product = context.Products
                .AsNoTracking()
                .First(p => p.Name == "No-Track Product");

            // Verify it's not tracked
            var state = context.Entry(product).State;
            Assert.AreEqual(EntityState.Detached, state, "AsNoTracking entities are Detached");

            // BUG: Modify the no-tracked entity
            product.Price = 150m;

            // SaveChanges won't persist the change (entity not tracked)
            context.SaveChanges();
        }

        // Verify: Price should still be original (change wasn't saved)
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.First(p => p.Name == "No-Track Product");
            
            // This will FAIL - the price wasn't saved
            Assert.AreEqual(150m, product.Price, 
                "AsNoTracking entities cannot be modified and saved. The entity is not tracked!");
        }
    }

    [Tutorial(
        title: "Tracking the Same Entity Twice Causes Conflict",
        category: "No-Tracking Queries",
        concept: @"EF Core's change tracker can only track ONE instance of an entity with a given primary key. If you try to track the same entity twice, you get an InvalidOperationException.

Common scenarios that cause this:
- Query the same entity twice in the same context
- Update an entity, then query it again
- Attach an entity that's already being tracked
- Multiple includes or joins returning the same entity

The error: 'The instance of entity type 'X' cannot be tracked because another instance with the same key value is already being tracked.'

This is EF Core's way of preventing data inconsistency.",
        pitfall: @"**Common Mistake:** Querying the same entity twice and trying to track both instances.

This test demonstrates:
1. Query a product (tracked)
2. Query the same product again (creates conflict)
3. EF Core throws InvalidOperationException

The test FAILS because you cannot track the same entity twice.",
        fix: @"**Solution:** Use AsNoTracking() for queries when entity is already tracked:

```csharp
// First query: Tracked
var product1 = context.Products.Find(1);

// Second query: Use AsNoTracking to avoid conflict
var product2 = context.Products
    .AsNoTracking()
    .First(p => p.Id == 1);

// Now both can coexist without conflict
```

Or use the same tracked instance:
```csharp
var product = context.Products.Find(1);
// Use 'product' everywhere, don't re-query
```

Or clear tracking:
```csharp
context.ChangeTracker.Clear(); // Detach everything
```",
        order: 31
    )]
    public static void Test_Tracking_Same_Entity_Twice_Throws_Exception()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Tracking_Conflict")
            .Options;

        // Setup: Add a product
        using (var context = new AppDbContext(options))
        {
            context.Products.Add(new Product { Name = "Conflict Product", Price = 75m, Stock = 20 });
            context.SaveChanges();
        }

        // Test: Track the same entity twice
        using (var context = new AppDbContext(options))
        {
            // First query: Tracked
            var product1 = context.Products.First(p => p.Name == "Conflict Product");
            var state1 = context.Entry(product1).State;
            Assert.AreEqual(EntityState.Unchanged, state1, "First query is tracked");

            // BUG: Second query of the same entity (also tracked)
            try
            {
                var product2 = context.Products.First(p => p.Name == "Conflict Product");
                
                // With In-Memory, this might not throw (limitation)
                // But with real databases, you'd get InvalidOperationException
                
                // If we get here, it's because In-Memory doesn't enforce this strictly
                Assert.IsTrue(false,
                    "Cannot track the same entity twice! " +
                    "Use AsNoTracking() for the second query or reuse the first instance.");
            }
            catch (InvalidOperationException ex)
            {
                // Expected with real databases
                Assert.IsTrue(ex.Message.Contains("already being tracked"),
                    "Correct exception: Cannot track same entity twice");
                
                // Test 'fails' to teach the lesson
                Assert.IsTrue(false,
                    "EF Core prevents tracking the same entity twice. " +
                    "Use AsNoTracking() for read-only queries or reuse tracked instances.");
            }
        }
    }

    [Tutorial(
        title: "Update Conflict: Attaching Entity Already Being Tracked",
        category: "No-Tracking Queries",
        concept: @"A common scenario for tracking conflicts is the 'disconnected entity' pattern:
1. Load entity in one context (e.g., web request)
2. Modify it in memory (e.g., user edits form)
3. Try to save it in a new context (e.g., next request)

If you're not careful, the entity might already be tracked when you try to attach/update it.

Methods that trigger tracking:
- Attach(entity)
- Update(entity)
- Entry(entity).State = Modified
- Find(), First(), etc. (without AsNoTracking)

All these fail if the entity is already tracked.",
        pitfall: @"**Common Mistake:** Loading an entity, then trying to attach/update a different instance with the same ID.

This test demonstrates:
1. Load a product (tracked)
2. Create a new instance with the same ID (disconnected)
3. Try to update/attach the new instance
4. Tracking conflict occurs!

The test FAILS to show this common web application scenario.",
        fix: @"**Solution - Several approaches:**

**1. Use AsNoTracking when loading:**
```csharp
var product = context.Products
    .AsNoTracking()
    .First(p => p.Id == id);
// Now can attach modified version later
```

**2. Use ChangeTracker.Clear():**
```csharp
var product = context.Products.Find(id);
context.ChangeTracker.Clear(); // Detach everything
context.Update(modifiedProduct); // Now works
context.SaveChanges();
```

**3. Check and modify existing tracked entity:**
```csharp
var tracked = context.Products.Local.FirstOrDefault(p => p.Id == id);
if (tracked != null)
{
    context.Entry(tracked).CurrentValues.SetValues(modifiedProduct);
}
else
{
    context.Update(modifiedProduct);
}
```

**4. Detach specific entity:**
```csharp
context.Entry(existingProduct).State = EntityState.Detached;
context.Update(modifiedProduct);
```",
        order: 32
    )]
    public static void Test_Attaching_Already_Tracked_Entity_Fails()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Attach_Conflict")
            .Options;

        int productId;

        // Setup: Add a product
        using (var context = new AppDbContext(options))
        {
            var product = new Product { Name = "Update Conflict Product", Price = 50m, Stock = 15 };
            context.Products.Add(product);
            context.SaveChanges();
            productId = product.Id;
        }

        // Test: Disconnected entity scenario
        using (var context = new AppDbContext(options))
        {
            // Load the entity (now it's tracked)
            var trackedProduct = context.Products.Find(productId);
            Assert.IsNotNull(trackedProduct, "Product loaded and tracked");

            // Simulate disconnected scenario: Create a new instance with same ID
            // (In real apps, this comes from a form submission, API call, etc.)
            var disconnectedProduct = new Product
            {
                Id = productId,
                Name = "Updated Name",
                Price = 99m,
                Stock = 25
            };

            // BUG: Try to update/attach the disconnected entity
            try
            {
                context.Update(disconnectedProduct);
                
                // This might work with In-Memory (limitation)
                // but would fail with real database
                Assert.IsTrue(false,
                    "Cannot attach/update entity that's already tracked! " +
                    "Use AsNoTracking, ChangeTracker.Clear(), or update the tracked instance.");
            }
            catch (InvalidOperationException ex)
            {
                // Expected with real databases
                Assert.IsTrue(ex.Message.Contains("already being tracked"),
                    "Correct: Cannot attach entity with same key as tracked entity");
                
                // Test 'fails' to teach the lesson
                Assert.IsTrue(false,
                    "This is a common web application scenario. " +
                    "Solutions: Use AsNoTracking, clear tracker, or update tracked entity's values.");
            }
        }
    }

    [Tutorial(
        title: "AsNoTracking Improves Performance for Read-Only Queries",
        category: "No-Tracking Queries",
        concept: @"AsNoTracking() provides significant performance benefits for read-only scenarios:

**Performance improvements:**
- No snapshot creation (saves memory)
- No change detection overhead
- Faster queries (less processing)
- Lower memory usage (no tracking data structures)
- Better for large result sets

**When to use AsNoTracking:**
- Read-only displays (list views, reports)
- DTOs that will be transformed
- Data that won't be modified
- Large result sets
- Performance-critical queries

**When NOT to use:**
- You need to modify the entity
- You need to load related entities and navigate them
- You're updating data

Global setting: `optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)`",
        pitfall: @"**Common Mistake:** Not using AsNoTracking() for read-only queries, wasting resources.

This test demonstrates:
1. Query without AsNoTracking (tracked, slower)
2. Query with AsNoTracking (not tracked, faster)
3. Both return data, but performance differs

The test intentionally shows the 'problem' of over-tracking.",
        fix: @"**Solution:** Use AsNoTracking for read-only queries:

```csharp
// Read-only list display
var products = context.Products
    .AsNoTracking()
    .Where(p => p.Stock > 0)
    .ToList();

// Read-only with includes
var orders = context.Orders
    .AsNoTracking()
    .Include(o => o.Items)
    .Where(o => o.OrderDate > startDate)
    .ToList();

// Set globally for read-heavy applications
optionsBuilder.UseQueryTrackingBehavior(
    QueryTrackingBehavior.NoTracking);

// Override per query when needed
var tracked = context.Products.AsTracking().First();
```

Benchmark your queries to see the impact (especially with large datasets).",
        order: 33
    )]
    public static void Test_AsNoTracking_Performance_Benefit()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_NoTracking_Performance")
            .Options;

        // Setup: Add many products
        using (var context = new AppDbContext(options))
        {
            for (int i = 1; i <= 100; i++)
            {
                context.Products.Add(new Product 
                { 
                    Name = $"Product {i}", 
                    Price = i * 10m, 
                    Stock = i 
                });
            }
            context.SaveChanges();
        }

        // Test: Query with tracking (default)
        using (var context = new AppDbContext(options))
        {
            var trackedProducts = context.Products.ToList();
            
            // All entities are tracked (memory overhead, change detection)
            var trackedCount = context.ChangeTracker.Entries().Count();
            Assert.AreEqual(100, trackedCount, "All 100 products are tracked");
        }

        // Test: Query with AsNoTracking
        using (var context = new AppDbContext(options))
        {
            var noTrackProducts = context.Products
                .AsNoTracking()
                .ToList();
            
            // No entities are tracked (better performance)
            var trackedCount = context.ChangeTracker.Entries().Count();
            Assert.AreEqual(0, trackedCount, "No products tracked with AsNoTracking");
            
            // We still get all the data!
            Assert.AreEqual(100, noTrackProducts.Count, "Still get all 100 products");
        }

        // The test 'fails' to emphasize the lesson
        using (var context = new AppDbContext(options))
        {
            // BUG: Using tracking query when we don't need to modify anything
            var products = context.Products
                .Where(p => p.Price > 500m)
                .ToList();
            
            // Check if entities are being tracked
            var trackedCount = context.ChangeTracker.Entries<Product>().Count();
            var isTracking = trackedCount > 0;
            
            // This creates unnecessary tracking overhead
            Assert.IsTrue(!isTracking,
                "Use AsNoTracking() for read-only queries! " +
                "It improves performance by avoiding change tracking overhead. " +
                $"Currently tracking {trackedCount} products unnecessarily. " +
                "Only use tracking queries when you need to modify entities.");
        }
    }

    [Tutorial(
        title: "AsNoTrackingWithIdentityResolution Prevents Duplicate Instances",
        category: "No-Tracking Queries",
        concept: @"EF Core 5.0+ introduced AsNoTrackingWithIdentityResolution():

**AsNoTracking():**
- Doesn't track entities
- Can return multiple instances of the same entity (different objects, same ID)
- Best performance
- Problem: Navigation properties might point to different instances

**AsNoTrackingWithIdentityResolution():**
- Doesn't track entities for SaveChanges
- Returns only ONE instance per entity ID (identity resolution)
- Navigation properties reference the same instance
- Slightly slower than AsNoTracking, faster than tracking
- Useful for queries with includes/joins

This solves a common problem with AsNoTracking and related entities.",
        pitfall: @"**Common Mistake:** Using AsNoTracking() with includes and getting duplicate instances of the same entity.

This test demonstrates:
1. Query orders with items using AsNoTracking
2. Multiple OrderItems might reference different Product instances with same ID
3. This can cause confusion in business logic

The test shows when identity resolution matters.",
        fix: @"**Solution:** Use AsNoTrackingWithIdentityResolution when you need consistent instances:

```csharp
// Problem: Multiple instances of same entity
var orders = context.Orders
    .AsNoTracking()
    .Include(o => o.Items)
        .ThenInclude(i => i.Product)
    .ToList();
// OrderItems might have different Product instances with same ID!

// Solution: Identity resolution
var orders = context.Orders
    .AsNoTrackingWithIdentityResolution()
    .Include(o => o.Items)
        .ThenInclude(i => i.Product)
    .ToList();
// OrderItems share the same Product instance per ID

// Or just use tracking if you need modifications
var orders = context.Orders
    .Include(o => o.Items)
        .ThenInclude(i => i.Product)
    .ToList();
```

Choose based on your needs: Performance vs. Identity consistency.",
        order: 34
    )]
    public static void Test_NoTracking_Can_Create_Duplicate_Instances()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Identity_Resolution")
            .Options;

        int productId;

        // Setup: Create product and multiple orders referencing it
        using (var context = new AppDbContext(options))
        {
            var product = new Product { Name = "Shared Product", Price = 100m, Stock = 50 };
            context.Products.Add(product);
            context.SaveChanges();
            productId = product.Id;

            var order1 = new Order
            {
                CustomerName = "Customer 1",
                OrderDate = DateTime.Now,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = productId, Quantity = 2, PriceAtOrder = 100m }
                }
            };

            var order2 = new Order
            {
                CustomerName = "Customer 2",
                OrderDate = DateTime.Now,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = productId, Quantity = 3, PriceAtOrder = 100m }
                }
            };

            context.Orders.AddRange(order1, order2);
            context.SaveChanges();
        }

        // Test: AsNoTracking might create duplicate instances
        using (var context = new AppDbContext(options))
        {
            var orders = context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .ToList();

            var product1 = orders[0].Items[0].Product;
            var product2 = orders[1].Items[0].Product;

            // With AsNoTracking, these might be different instances (same ID, different objects)
            // With AsNoTrackingWithIdentityResolution, they'd be the same instance
            // With tracking, they'd be the same instance
            
            var areSameInstance = ReferenceEquals(product1, product2);
            
            // This might be true or false depending on EF behavior
            // Test 'fails' to teach about identity resolution
            Assert.IsTrue(areSameInstance,
                "AsNoTracking() can create multiple instances of the same entity. " +
                "Use AsNoTrackingWithIdentityResolution() if you need consistent instances, " +
                "or use regular tracking queries.");
        }
    }
}
