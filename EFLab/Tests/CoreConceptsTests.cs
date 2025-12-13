using Microsoft.EntityFrameworkCore;
using EFLab.Testing;
using EFLab.Models;
using EFLab.Data;

namespace EFLab.Tests;

/// <summary>
/// Core Concepts: DbContext lifecycle, entity states, and change tracking
/// </summary>
public static class CoreConceptsTests
{
    [Tutorial(
        title: "SaveChanges Is Required for Persistence",
        category: "Core Concepts",
        concept: @"Entity Framework Core uses a Unit of Work pattern through DbContext. Changes to tracked entities are kept in memory until SaveChanges() is called. This batches database operations for efficiency.

Key points:
- Add(), Update(), Remove() only modify the change tracker in memory
- No database operations occur until SaveChanges()
- SaveChanges() generates and executes SQL statements as a batch
- Returns the number of entities affected",
        pitfall: @"**Common Mistake:** Developers forget to call SaveChanges() and expect data to be persisted automatically.

This test intentionally:
1. Creates a new Product
2. Adds it to the DbContext
3. Does NOT call SaveChanges()
4. Expects to find the product in a new query

Result: The test FAILS because the product was never saved to the database.",
        fix: @"**Solution:** Always call SaveChanges() after modifying entities.

```csharp
context.Products.Add(product);
await context.SaveChangesAsync(); // or context.SaveChanges()
```

The test will pass once SaveChanges() is added after the Add() operation.",
        order: 1
    )]
    public static void Test_SaveChanges_Required_For_Persistence()
    {
        // Arrange: Create in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_SaveChanges_Required")
            .Options;

        using var context = new AppDbContext(options);

        // Act: Add a product WITHOUT calling SaveChanges
        var product = new Product
        {
            Name = "Laptop",
            Price = 999.99m,
            Stock = 10
        };

        context.Products.Add(product);
        // BUG: Missing SaveChanges()!

        // Try to query the product
        var foundProduct = context.Products.FirstOrDefault(p => p.Name == "Laptop");

        // Assert: This will FAIL because SaveChanges was not called
        Assert.IsNotNull(foundProduct, "Product should be found after adding to context");
    }

    [Tutorial(
        title: "Entity State Tracking",
        category: "Core Concepts",
        concept: @"EF Core tracks the state of each entity in the DbContext. Entity states:

- **Detached**: Not tracked by the context
- **Added**: New entity, will be inserted on SaveChanges()
- **Unchanged**: Tracked, no modifications detected
- **Modified**: Tracked, changes detected, will be updated on SaveChanges()
- **Deleted**: Marked for deletion, will be deleted on SaveChanges()

You can check state with: `context.Entry(entity).State`",
        pitfall: @"**Common Mistake:** Assuming entities are automatically tracked or not understanding when state changes.

This test demonstrates:
1. Creating an entity (Detached state)
2. Adding it to context (Added state)
3. After SaveChanges (Unchanged state)
4. Modifying a property (Modified state)

The test intentionally checks the wrong state at each step.",
        fix: @"**Solution:** Understand the entity lifecycle:

```csharp
var product = new Product(); // Detached
context.Products.Add(product); // Added
context.SaveChanges(); // Now Unchanged
product.Price = 100; // Now Modified
context.SaveChanges(); // Back to Unchanged
```",
        order: 2
    )]
    public static void Test_Entity_State_Lifecycle()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Entity_State")
            .Options;

        using var context = new AppDbContext(options);

        // Step 1: New entity is Detached
        var product = new Product { Name = "Mouse", Price = 25.99m, Stock = 50 };
        var state1 = context.Entry(product).State;
        
        // BUG: This will FAIL - new entities are Detached, not Added
        Assert.AreEqual(EntityState.Added, state1, "New entity should be Detached until added to context");

        // Step 2: After Add(), state is Added
        context.Products.Add(product);
        var state2 = context.Entry(product).State;
        Assert.AreEqual(EntityState.Added, state2, "After Add(), state should be Added");

        // Step 3: After SaveChanges(), state is Unchanged
        context.SaveChanges();
        var state3 = context.Entry(product).State;
        Assert.AreEqual(EntityState.Unchanged, state3, "After SaveChanges(), state should be Unchanged");

        // Step 4: After modifying, state is Modified
        product.Price = 29.99m;
        var state4 = context.Entry(product).State;
        Assert.AreEqual(EntityState.Modified, state4, "After modification, state should be Modified");
    }

    [Tutorial(
        title: "Change Tracking Detects Property Changes",
        category: "Core Concepts",
        concept: @"EF Core's change tracker automatically detects when properties of tracked entities change. It does this by:

1. Taking a snapshot of property values when an entity is first tracked
2. Comparing current values to the snapshot when SaveChanges() is called
3. Generating UPDATE statements only for modified properties

This is called 'snapshot change tracking' and is the default for most entity types.",
        pitfall: @"**Common Mistake:** Modifying an entity and expecting immediate database updates without SaveChanges().

This test:
1. Loads a product from the database
2. Modifies its price property
3. Checks if the change is detected (it is)
4. But expects the database to be updated immediately (it's not)

The test FAILS because changes are only in memory until SaveChanges().",
        fix: @"**Solution:** Call SaveChanges() to persist modifications:

```csharp
var product = context.Products.First();
product.Price = 199.99m;
// Change is tracked but NOT in database yet
context.SaveChanges(); // NOW it's persisted
```",
        order: 3
    )]
    public static void Test_Change_Tracking_Detects_Modifications()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Change_Tracking")
            .Options;

        // Setup: Add a product
        using (var context = new AppDbContext(options))
        {
            context.Products.Add(new Product { Name = "Keyboard", Price = 79.99m, Stock = 30 });
            context.SaveChanges();
        }

        // Test: Modify and check tracking
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.First(p => p.Name == "Keyboard");
            var originalPrice = product.Price;

            // Modify the price
            product.Price = 89.99m;

            // Check if modification is detected
            var isModified = context.Entry(product).State == EntityState.Modified;
            Assert.IsTrue(isModified, "Change tracker should detect the price modification");

            // BUG: Expecting database to be updated immediately (it's not!)
            // We need a new context to check the database state
        }

        // Check if change persisted (it won't - SaveChanges was never called!)
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.First(p => p.Name == "Keyboard");
            
            // This will FAIL - the modified price was never saved
            Assert.AreEqual(89.99m, product.Price, "Price should be updated in database after SaveChanges()");
        }
    }
}
