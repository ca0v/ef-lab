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

    [Tutorial(
        title: "Delete Operations Require SaveChanges",
        category: "Core Concepts",
        concept: @"Deleting entities in EF Core follows the same Unit of Work pattern. The Remove() method marks an entity for deletion, but doesn't execute the DELETE statement immediately.

Key points:
- Remove() marks entity as Deleted in the change tracker
- RemoveRange() can delete multiple entities at once
- The database DELETE only happens on SaveChanges()
- The entity remains in memory (marked as Deleted) until SaveChanges()
- After SaveChanges(), the entity state becomes Detached",
        pitfall: @"**Common Mistake:** Calling Remove() and expecting the entity to be immediately deleted from the database.

This test:
1. Adds a product and saves it
2. Calls Remove() on the product
3. Checks the entity state (correctly shows Deleted)
4. Queries in a new context expecting the product to be gone (it's not!)

The test FAILS because SaveChanges() was never called after Remove().",
        fix: @"**Solution:** Call SaveChanges() after Remove() to persist the deletion:

```csharp
var product = context.Products.First();
context.Products.Remove(product);
context.SaveChanges(); // NOW it's deleted from database
```",
        order: 4
    )]
    public static void Test_Delete_Requires_SaveChanges()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Delete_Operation")
            .Options;

        // Setup: Add a product
        using (var context = new AppDbContext(options))
        {
            context.Products.Add(new Product { Name = "Headphones", Price = 59.99m, Stock = 15 });
            context.SaveChanges();
        }

        // Test: Delete without SaveChanges
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.First(p => p.Name == "Headphones");
            
            // Mark for deletion
            context.Products.Remove(product);
            
            // Check state is Deleted (this is correct)
            var state = context.Entry(product).State;
            Assert.AreEqual(EntityState.Deleted, state, "After Remove(), state should be Deleted");
            
            // BUG: Not calling SaveChanges()!
        }

        // Verify deletion (it won't be deleted - SaveChanges was never called!)
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.FirstOrDefault(p => p.Name == "Headphones");
            
            // This will FAIL - the product is still in the database
            Assert.IsNull(product, "Product should be deleted from database after SaveChanges()");
        }
    }

    [Tutorial(
        title: "Querying One-to-Many Relationships with Include",
        category: "Core Concepts - Relationships",
        concept: @"EF Core uses lazy loading by default, but explicitly loading related data requires the Include() method. This performs a JOIN to load parent and child entities together.

Key points:
- Include() eagerly loads navigation properties
- Without Include(), navigation properties are null (unless lazy loading is enabled)
- Can chain multiple Include() calls for different relationships
- Use ThenInclude() for nested relationships (grandchildren)
- This generates a SQL JOIN statement",
        pitfall: @"**Common Mistake:** Accessing navigation properties without using Include() and getting null or empty collections.

This test:
1. Creates an Order with OrderItems
2. Saves everything correctly
3. Queries the Order WITHOUT Include()
4. Expects the Items collection to be populated (it's not!)

The test FAILS because related data must be explicitly loaded with Include().",
        fix: @"**Solution:** Use Include() to load related entities:

```csharp
var order = context.Orders
    .Include(o => o.Items)  // Load the Items collection
    .First(o => o.CustomerName == ""John"");

// Now order.Items is populated
```",
        order: 5
    )]
    public static void Test_OneToMany_Query_Requires_Include()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_OneToMany_Query")
            .Options;

        // Setup: Create an order with items
        using (var context = new AppDbContext(options))
        {
            var product1 = new Product { Name = "Monitor", Price = 299.99m, Stock = 10 };
            var product2 = new Product { Name = "Mouse", Price = 29.99m, Stock = 50 };
            context.Products.AddRange(product1, product2);
            context.SaveChanges();

            var order = new Order
            {
                CustomerName = "John Doe",
                OrderDate = DateTime.Now,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = product1.Id, Quantity = 1, PriceAtOrder = 299.99m },
                    new OrderItem { ProductId = product2.Id, Quantity = 2, PriceAtOrder = 29.99m }
                }
            };
            context.Orders.Add(order);
            context.SaveChanges();
        }

        // Test: Query without Include
        using (var context = new AppDbContext(options))
        {
            // BUG: Querying without Include()
            var order = context.Orders.First(o => o.CustomerName == "John Doe");
            
            // This will FAIL - Items collection is empty without Include()
            Assert.AreEqual(2, order.Items.Count, "Order should have 2 items when loaded with Include()");
        }
    }

    [Tutorial(
        title: "Adding Related Entities (One-to-Many)",
        category: "Core Concepts - Relationships",
        concept: @"When adding entities with relationships, EF Core can track both parent and child entities if they're properly connected. You can add the parent with navigation properties populated, and EF will handle the foreign keys.

Key points:
- Adding a parent entity with populated navigation properties adds all related children
- EF Core automatically sets foreign key values
- The order of operations matters less when using navigation properties
- All entities must be in the same context
- Still requires SaveChanges() to persist everything",
        pitfall: @"**Common Mistake:** Adding related entities but forgetting SaveChanges(), or adding them to different contexts.

This test:
1. Creates an Order with OrderItems through navigation properties
2. Adds the Order to the context
3. Does NOT call SaveChanges()
4. Tries to query the order and items in a new context

The test FAILS because nothing was persisted to the database.",
        fix: @"**Solution:** Add parent with children, then call SaveChanges():

```csharp
var order = new Order
{
    CustomerName = ""Jane"",
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = 1, Quantity = 3 }
    }
};
context.Orders.Add(order);
context.SaveChanges(); // Persists both order and items
```",
        order: 6
    )]
    public static void Test_Adding_Related_Entities_Requires_SaveChanges()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Adding_Related")
            .Options;

        // Setup: Create a product first
        using (var context = new AppDbContext(options))
        {
            context.Products.Add(new Product { Name = "Tablet", Price = 399.99m, Stock = 20 });
            context.SaveChanges();
        }

        // Test: Add order with items but don't call SaveChanges
        using (var context = new AppDbContext(options))
        {
            var product = context.Products.First(p => p.Name == "Tablet");
            
            var order = new Order
            {
                CustomerName = "Jane Smith",
                OrderDate = DateTime.Now,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Quantity = 2, PriceAtOrder = 399.99m }
                }
            };
            
            context.Orders.Add(order);
            // BUG: Not calling SaveChanges()!
        }

        // Verify (this will fail - nothing was saved)
        using (var context = new AppDbContext(options))
        {
            var order = context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.CustomerName == "Jane Smith");
            
            // This will FAIL - order was never persisted
            Assert.IsNotNull(order, "Order should exist after SaveChanges()");
        }
    }

    [Tutorial(
        title: "Deleting Parent Deletes Children (Cascade Delete)",
        category: "Core Concepts - Relationships",
        concept: @"By default, EF Core configures cascade delete for required relationships. When you delete a parent entity, all dependent child entities are automatically deleted.

Key points:
- Cascade delete is the default for required (non-nullable foreign key) relationships
- Children are marked as Deleted when parent is deleted
- All deletes happen in one transaction on SaveChanges()
- You can configure different cascade behaviors (Restrict, SetNull, etc.)
- The children don't need to be loaded for cascade delete to work",
        pitfall: @"**Common Mistake:** Deleting a parent entity but forgetting SaveChanges(), or not understanding cascade behavior.

This test:
1. Creates an Order with OrderItems
2. Saves everything properly
3. Loads and deletes the Order
4. Does NOT call SaveChanges()
5. Expects both order and items to be gone

The test FAILS because SaveChanges() was never called after the delete.",
        fix: @"**Solution:** Call SaveChanges() after deleting the parent:

```csharp
var order = context.Orders.Find(orderId);
context.Orders.Remove(order);
context.SaveChanges(); // Deletes order AND all OrderItems
```

Note: You don't need to load Items to delete them - cascade handles it.",
        order: 7
    )]
    public static void Test_Cascade_Delete_Requires_SaveChanges()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Cascade_Delete")
            .Options;

        int orderId;

        // Setup: Create order with items
        using (var context = new AppDbContext(options))
        {
            var product = new Product { Name = "Webcam", Price = 79.99m, Stock = 25 };
            context.Products.Add(product);
            context.SaveChanges();

            var order = new Order
            {
                CustomerName = "Bob Wilson",
                OrderDate = DateTime.Now,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Quantity = 1, PriceAtOrder = 79.99m }
                }
            };
            context.Orders.Add(order);
            context.SaveChanges();
            orderId = order.Id;
        }

        // Test: Delete parent without SaveChanges
        using (var context = new AppDbContext(options))
        {
            var order = context.Orders.Find(orderId);
            Assert.IsNotNull(order, "Order should exist before deletion");
            
            context.Orders.Remove(order!);
            // BUG: Not calling SaveChanges()!
        }

        // Verify deletion (this will fail - nothing was deleted)
        using (var context = new AppDbContext(options))
        {
            var order = context.Orders.FirstOrDefault(o => o.Id == orderId);
            var items = context.OrderItems.Where(oi => oi.OrderId == orderId).ToList();
            
            // These will FAIL - order and items still exist
            Assert.IsNull(order, "Order should be deleted after SaveChanges()");
            Assert.Empty(items, "OrderItems should be cascade deleted with parent Order");
        }
    }
}
