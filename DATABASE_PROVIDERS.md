# Database Provider Comparison

## InMemory vs SQLite: Which Should You Use?

This tutorial supports two database providers. Understanding their differences helps you choose the right one for learning specific EF Core concepts.

## Quick Comparison

| Feature | InMemory | SQLite |
|---------|----------|--------|
| **Speed** | ⚡ Very Fast | 🐢 Slower |
| **Setup** | ✅ None | ✅ Automatic |
| **Foreign Keys** | ❌ Not enforced | ✅ Enforced |
| **Transactions** | ⚠️ Fake (no isolation) | ✅ Real |
| **Cascade Delete** | ⚠️ Unreliable | ✅ Works correctly |
| **Constraints** | ❌ Not enforced | ✅ Enforced |
| **Persistence** | 💾 In-memory only | 💾 File-based |
| **Use For** | 🎓 Learning basics | 🎓 Realistic testing |

## Detailed Differences

### Foreign Key Constraints

**InMemory:**
```csharp
// InMemory allows this (shouldn't!)
var item = new OrderItem { OrderId = 999, ProductId = 888 };
context.OrderItems.Add(item);
context.SaveChanges(); // ✅ Succeeds (but shouldn't!)
```

**SQLite:**
```csharp
// SQLite correctly enforces foreign keys
var item = new OrderItem { OrderId = 999, ProductId = 888 };
context.OrderItems.Add(item);
context.SaveChanges(); // ❌ Throws DbUpdateException
```

**Lesson:** InMemory doesn't validate that foreign keys reference existing records.

### Transactions

**InMemory:**
```csharp
// InMemory transactions don't provide isolation
using var transaction = context.Database.BeginTransaction();
// Changes visible to other contexts immediately!
context.SaveChanges();
transaction.Rollback(); // ⚠️ Rollback may not work as expected
```

**SQLite:**
```csharp
// SQLite provides real transaction isolation
using var transaction = context.Database.BeginTransaction();
context.SaveChanges(); // Changes isolated until commit
transaction.Rollback(); // ✅ All changes rolled back correctly
```

**Lesson:** Use SQLite for testing transaction behavior.

### Cascade Delete

**InMemory:**
```csharp
// InMemory cascade delete is unreliable
var order = context.Orders.Find(orderId);
context.Orders.Remove(order);
context.SaveChanges();
// ⚠️ OrderItems may or may not be deleted
```

**SQLite:**
```csharp
// SQLite cascade delete works correctly
var order = context.Orders.Find(orderId);
context.Orders.Remove(order);
context.SaveChanges();
// ✅ All OrderItems are properly deleted
```

**Lesson:** Use SQLite for testing cascade delete behavior.

### Unique Constraints

**InMemory:**
```csharp
// InMemory allows duplicate unique values
context.Users.Add(new User { Email = "test@example.com" });
context.Users.Add(new User { Email = "test@example.com" });
context.SaveChanges(); // ✅ Succeeds (but shouldn't!)
```

**SQLite:**
```csharp
// SQLite enforces unique constraints
context.Users.Add(new User { Email = "test@example.com" });
context.Users.Add(new User { Email = "test@example.com" });
context.SaveChanges(); // ❌ Throws DbUpdateException
```

**Lesson:** InMemory doesn't enforce unique constraints.

## When to Use Each Provider

### Use InMemory For:
✅ Learning EF Core basics (CRUD operations)  
✅ Understanding DbContext lifecycle  
✅ Learning entity states (Added, Modified, etc.)  
✅ Understanding change tracking  
✅ Quick prototyping  
✅ Unit tests where database behavior doesn't matter  

**Example topics:**
- SaveChanges requirements
- Entity state tracking
- Change tracking
- Include() for loading related data
- AsNoTracking() queries
- Basic many-to-many relationships

### Use SQLite For:
✅ Testing transaction behavior  
✅ Testing cascade delete  
✅ Testing foreign key constraints  
✅ Testing unique constraints  
✅ Realistic integration tests  
✅ Understanding real database limitations  

**Example topics:**
- Transaction isolation
- Transaction rollback
- Cascade delete behavior
- Foreign key constraint violations
- Concurrency conflicts
- Database-enforced constraints

## Running Tests

### InMemory (Default)
```bash
# Run all tests
npm test

# Run specific category
npm run test:pattern -- "Core Concepts"

# Run specific test
npm run test:pattern -- "SaveChanges"
```

### SQLite
```bash
# Run all tests
npm run test:sqlite

# Run specific category  
npm run test:sqlite:pattern -- "Transactions"

# Run specific test
npm run test:sqlite:pattern -- "Cascade Delete"
```

## Comparison Example

Try this to see the difference:

```bash
# InMemory: Some transaction tests pass incorrectly
npm run test:pattern -- "Transaction"

# SQLite: Same tests show real transaction behavior
npm run test:sqlite:pattern -- "Transaction"
```

## Our Recommendation

1. **Start with InMemory** - Learn EF Core fundamentals quickly
2. **Switch to SQLite** when you reach:
   - Transaction tests
   - Cascade delete tests
   - Foreign key tests
   - Any test that says "Use SQLite for realistic testing"
3. **Use SQLite for real projects** - Always use a real database in production!

## Performance Note

InMemory is faster because:
- No file I/O
- No SQL generation
- No constraint checking
- No transaction overhead

But this speed comes at the cost of not catching real database issues during testing!

## Cleanup

SQLite creates `.db` files:
```bash
# These are automatically cleaned up when tests complete
test_*.db
test_*.db-shm  # Shared memory file
test_*.db-wal  # Write-ahead log

# Files are in .gitignore - won't be committed
```

InMemory requires no cleanup - everything is in memory and disappears when the context is disposed.
