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

