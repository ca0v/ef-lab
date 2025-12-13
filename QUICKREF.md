# Quick Reference

## Running Tests

```bash
# If you've added .NET to your PATH permanently:
npm test                              # All tests
npm run test:pattern -- "pattern"    # Match pattern
npm run watch                        # Watch mode

# If you haven't modified PATH:
./npm-dotnet.sh test
./npm-dotnet.sh run test:pattern -- "pattern"
./npm-dotnet.sh run watch
```

## Common Commands

```bash
# Build
npm run build

# Clean build artifacts
npm run clean

# Check for errors
npm run build

# Entity Framework
npm run ef:list                              # Show EF commands
npm run ef:migrations:add -- InitialCreate   # Add migration
npm run ef:migrations:list                   # List migrations
npm run ef:database:update                   # Apply migrations
npm run ef:database:drop                     # Drop database
```

## Project Structure

```
EFLab/
├── EFLab.csproj           # Project file with EF Core packages
├── Program.cs              # Test runner entry point
└── Testing/
    ├── Assert.cs           # Custom assertion library
    └── TestRunner.cs       # Test execution engine
```

## Writing Tests

Tests are registered in `Program.cs`:

```csharp
runner.RegisterTest(
    "Test_Name_Description",
    "Category",
    () =>
    {
        // Your test code here
        Assert.IsTrue(condition, "message");
    }
);
```

## Available Assertions

- `Assert.IsTrue(bool, message?)`
- `Assert.IsFalse(bool, message?)`
- `Assert.AreEqual(expected, actual, message?)`
- `Assert.AreNotEqual(expected, actual, message?)`
- `Assert.IsNull(value, message?)`
- `Assert.IsNotNull(value, message?)`
- `Assert.Throws<TException>(action, message?)`
- `Assert.Contains(collection, item, message?)`
- `Assert.Empty(collection, message?)`
- `Assert.NotEmpty(collection, message?)`
- `Assert.Count(collection, expectedCount, message?)`

## Test Categories

- **CoreConcepts** - DbContext lifecycle, entity states, change tracking
- **InMemory** - In-memory database testing
- **Transactions** - Transaction usage and pitfalls
- **MultipleContexts** - Multiple DbContext instances
- **NoTracking** - AsNoTracking queries
- **ManyToMany** - Many-to-many relationships

## Tips

1. **All tests start failing intentionally** - This is by design!
2. **Read the error messages** - They teach you about EF pitfalls
3. **Fix one test at a time** - Focus on understanding each concept
4. **Use pattern matching** - Test specific areas: `npm run test:pattern -- "InMemory"`
5. **Watch mode is your friend** - See changes instantly: `npm run watch`
