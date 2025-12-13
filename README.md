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
   npm test                         # Run all tests
   npm run test:pattern -- "DbContext"  # Run specific tests
   ```

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

### Transactions (5 tests)
12. **Transactions Require Explicit Commit** - Forgetting to call Commit()
13. **Rollback Undoes All Changes** - Understanding transaction atomicity
14. **Nested Transactions with Sub-Contexts** - Sharing transactions across DbContext instances
15. **No Partial Commits** - Transaction is all-or-nothing
16. **In-Memory Transaction Limitations** - Why In-Memory doesn't truly support transactions

**Total: 16 failing tests** - All intentionally fail to teach EF Core pitfalls!

More tests coming soon! Check [TUTORIAL.md](TUTORIAL.md) for detailed explanations.

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