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

