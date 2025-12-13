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