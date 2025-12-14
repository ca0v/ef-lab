# Entity Framework Failing-First Tutorial

A hands-on Entity Framework Core tutorial where **all tests start failing intentionally**. Learn EF Core by understanding common pitfalls and fixing them yourself!

## Quick Start

1. **Install .NET SDK 8.0** (if not already installed)
   ```bash
   npm run install:dotnet:help  # View installation instructions
   # or
   npm run install:dotnet:script  # Auto-install on Linux
   ```

2. **Setup the project**
   ```bash
   npm run setup
   ```

3. **Add .NET to your PATH** (one-time setup)
   ```bash
   # Add to ~/.bashrc:
   export DOTNET_ROOT="$HOME/.dotnet"
   export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"
   
   # Then reload:
   source ~/.bashrc
   ```
   
   Or use the helper script: `./npm-dotnet.sh test`

4. **Run tests**
   ```bash
   npm test                         # Run all tests (InMemory)
   npm run test:pattern -- "DbContext"  # Run specific tests
   npm run test:sqlite              # Run all tests with SQLite
   npm run test:sqlite:pattern -- "Transaction"  # Run specific tests with SQLite
   ```

See [SETUP.md](SETUP.md) for detailed installation and usage instructions.

## Database Providers

This tutorial supports two database providers:

### InMemory (Default)
- **Pros**: Fast, no setup, great for learning basics
- **Cons**: Doesn't enforce foreign keys, transactions, or some constraints
- **Use for**: Core concepts, basic queries, learning EF Core fundamentals

### SQLite (Recommended for realistic testing)
- **Pros**: Real relational database, enforces constraints, supports transactions
- **Cons**: Slightly slower, creates `.db` files
- **Use for**: Testing transactions, cascade delete, foreign keys, realistic scenarios

**Switch to SQLite:**
```bash
npm run test:sqlite              # All tests with SQLite
npm run test:sqlite:pattern -- "Transaction"  # Specific category
```

**Why use SQLite?**
- InMemory provider doesn't enforce foreign key constraints
- InMemory doesn't truly support transactions
- InMemory cascade delete behavior differs from real databases
- Some tests will pass with InMemory but fail correctly with SQLite (teaching moments!)

**See [DATABASE_PROVIDERS.md](DATABASE_PROVIDERS.md) for detailed comparison.**

See [SETUP.md](SETUP.md) for detailed installation and usage instructions.

## Troubleshooting

**`dotnet-ef` fails with "Settings file was not found"**
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Reinstall: `npm run install:ef`

**`dotnet ef` says ".NET location: Not found"**
- Set `DOTNET_ROOT`: `export DOTNET_ROOT=$HOME/.dotnet`
- Add to PATH: `export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"`
- Or use: `./npm-dotnet.sh test`

## What Makes This Tutorial Different?

- **No xUnit/MSTest** - Custom assertion library to keep focus on EF Core concepts
- **Failing-First Approach** - Every example starts broken; you learn by fixing it
- **Real Pitfalls** - Common mistakes developers make with Entity Framework
- **In-Memory Testing** - Fast feedback loop using EF Core In-Memory provider
- **Documentation in Code** - Tutorial notes live in `[Tutorial]` attributes alongside tests
- **Auto-Generated Docs** - Run `npm run docs:generate` to create TUTORIAL.md from test attributes

## Current Tests

Run `npm test` to see all available tests. Currently includes:

### Core Concepts (7 tests)

**Basic Operations:**
1. **SaveChanges Is Required for Persistence** - Forgetting to call SaveChanges()
2. **Entity State Tracking** - Understanding entity lifecycle states
3. **Change Tracking Detects Property Changes** - How EF detects modifications
4. **Delete Operations Require SaveChanges** - Deleting entities properly

**One-to-Many Relationships:**
5. **Querying One-to-Many Relationships with Include** - Loading related data with Include()
6. **Adding Related Entities** - Inserting parent with children
7. **Deleting Parent Deletes Children (Cascade Delete)** - Understanding cascade behavior

### In-Memory Testing (4 tests)
8. **In-Memory Does Not Enforce Relational Constraints** - Foreign keys aren't validated
9. **In-Memory Persistence Across Contexts** - Database name management
10. **In-Memory Does Not Support Raw SQL** - FromSqlRaw limitations
11. **In-Memory Query Translation Differs** - LINQ behavior differences

### Transactions (8 tests)
12. **Transactions Require Explicit Commit** - Forgetting to call Commit()
13. **Rollback Undoes All Changes** - Understanding transaction atomicity
14. **Nested Transactions with Sub-Contexts** - Sharing transactions across DbContext instances
15. **No Partial Commits** - Transaction is all-or-nothing
16. **In-Memory Transaction Limitations** - Why In-Memory doesn't truly support transactions
17. **Sharing Transactions Across Multiple Contexts (The RIGHT Way)** - Using UseTransaction() correctly
18. **When Should You Use Sub-Contexts in Transactions?** - Repository/service layer patterns
19. **Nested Transactions Are Not Truly Nested** - Savepoints and transaction reality

### No-Tracking Queries (5 tests)
20. **No-Tracking Queries Cannot Be Modified and Saved** - AsNoTracking entities are read-only
21. **Tracking the Same Entity Twice Causes Conflict** - The common "already being tracked" error
22. **Update Conflict: Attaching Entity Already Being Tracked** - Disconnected entity pattern pitfall
23. **AsNoTracking Improves Performance** - When and why to use AsNoTracking
24. **AsNoTrackingWithIdentityResolution** - Preventing duplicate instances

### No-Tracking as Default Behavior (4 tests)
25. **Setting NoTracking as Global Default Behavior** - UseQueryTrackingBehavior configuration
26. **AsTracking() Overrides NoTracking Default** - Explicitly enabling tracking when needed
27. **NoTracking Default: Checking Tracking Status** - Verifying entity tracking state
28. **NoTracking Default with Add/Update/Delete Operations** - Understanding what NoTracking affects

### Many-to-Many Relationships (5 tests)
29. **Implicit Many-to-Many: Adding Duplicates Silently** - Preventing duplicate relationships (Student-Course)
30. **Explicit Many-to-Many: Required for Custom Join Data** - Join entities with additional properties (Employee-Project)
31. **Removing Many-to-Many Relationships Without Deleting Entities** - Proper relationship removal
32. **Cascading Many-to-Many: Deleting Entity Removes Relationships** - Cascade delete behavior
33. **Querying Many-to-Many: Include vs. ThenInclude** - Proper eager loading with Include/ThenInclude

**Total: 33 tests** - All intentionally fail to teach EF Core pitfalls!

More tests coming soon! Check [TUTORIAL.md](TUTORIAL.md) for detailed explanations.

## Coming Soon! 🚀

The following high-priority topics are planned for future test additions:

1. **N+1 Query Problem** ⭐⭐⭐
   - The most common performance pitfall in EF Core
   - Eager loading vs. Lazy loading vs. Explicit loading
   - AsSplitQuery() for one-to-many collections
   - Detecting N+1 queries

2. **Projections with Select()** ⭐⭐⭐
   - Performance optimization by selecting only needed columns
   - DTO patterns and avoiding over-fetching
   - Anonymous types vs. concrete DTOs
   - Projection limitations (what can/cannot be translated)

3. **Global Query Filters** ⭐⭐⭐
   - Soft delete patterns (IsDeleted filtering)
   - Multi-tenancy (TenantId filtering)
   - IgnoreQueryFilters() to bypass filters
   - Common pitfalls with filtered includes

4. **Indexing and Performance** ⭐⭐
   - Creating indexes (single and composite)
   - Understanding query execution with indexes
   - Index pitfalls (over-indexing, wrong columns)
   - Unique indexes and constraints

5. **Complex Queries and Gotchas** ⭐⭐
   - GroupBy() client vs. server evaluation
   - Subqueries and their limitations
   - LINQ methods that force client evaluation
   - Query splitting for cartesian explosion

Stay tuned for these exciting additions to the tutorial!

# Prompt

Resume building Entity Framework tutorial as failing-first unit tests using custom assertion library (no xUnit). Focus on pitfalls, fixes, in-memory DB. Structure per latest outline. All examples start failing; teach why and how to fix.

## Entity Framework Tutorial Outline

- **Core Concepts**  
  DbContext lifecycle, entity states, change tracking. Failing examples: Manual conflicts.

- **In-Memory Testing**  
  Setup, limitations. Failing: No persistence without SaveChanges.

- **Transactions**  
  Usage, nesting. Failing: Partial commits, deadlocks.

- **Multiple Contexts**  
  Scoping, sharing. Failing: Duplicate tracking (multiple examples). Long vs. short contexts pros/cons.

- **No-Tracking Queries**  
  AsNoTracking implications. Failing: Modify no-tracked entities.

- **Many-to-Many Relationships**  
  Implicit/explicit setups. User-Role, Role-FeatureFlag. Failing: Duplicates, missing custom data.

- **Troubleshooting**  
  Common exceptions, logging, fixes.

- **Additional Topics**  
  Concurrency, loading strategies, migrations, DI scoping.

- **dotnet EF Tool**  
  Commands, Model First vs. Database First.

- **Custom Testing Framework**  
  Basic assertions. All examples start failing; tutorial teaches fixes and reasons.

- **Project Management**  
  npm/package.json scripts. Custom CLI with --test "pattern" flag.