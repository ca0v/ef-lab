---
artifact: ef-lab
phase: design
depends-on: []
references: ["ef-lab.req"]
version: 1.0.0
last-updated: 2025-12-19
---

# EF Core Failing-First Tutorial - Design

## Overview
The high-level design approach for the EF Core Failing-First Tutorial adopts a modular, .NET-based architecture centered on a "failing-first" pedagogy. The system is structured as a console application with embedded unit tests that intentionally fail to teach EF Core concepts through error analysis and correction. Key design principles include separation of concerns (e.g., isolating EF operations from test logic), provider abstraction for database flexibility (InMemory vs. SQLite), and extensibility for adding modules. The design leverages .NET Core's cross-platform capabilities, integrates npm for build and execution scripting, and uses a custom assertion library to avoid external testing frameworks, ensuring focus on EF Core internals. Operational workflows emphasize iterative development: setup via npm, test execution with failure feedback, provider switching for constraint enforcement, and automatic documentation generation from test metadata. The system targets local development environments, prioritizing educational usability over production scalability, with built-in constraints to highlight EF Core pitfalls like state tracking and relationship handling.

## Architecture
The architecture follows a layered, component-based model inspired by clean architecture principles, with clear separation between UI, business logic, data access, and testing layers. The core is a .NET console application (targeting .NET 8.0) that encapsulates EF Core operations, with npm orchestrating external interactions.

### System Components
- **Test Runner Component**: A console executable that discovers and executes failing tests using reflection on test classes. It integrates with the custom assertion library to report failures and guide fixes. Supports CLI flags for pattern-based execution (e.g., filtering by "Transaction").
- **Custom Assertion Library**: A lightweight library providing assertion methods (e.g., AssertEqual, AssertThrows) without dependencies on xUnit/MSTest. It captures EF-specific errors (e.g., DbUpdateException) and formats them for educational feedback.
- **EF Core Operations Module**: Core logic for EF interactions, including DbContext management, entity tracking, queries, transactions, and relationships. Implements provider abstraction to switch between InMemory and SQLite dynamically.
- **Database Provider Abstraction Layer**: Abstracts database operations, allowing runtime switching via configuration. Handles setup for InMemory (no constraints) and SQLite (with foreign keys enabled).
- **Documentation Generator**: A utility that parses test attributes (e.g., custom [Explanation] attributes) to generate TUTORIAL.md, integrating post-test execution for explanations of fixes and best practices.
- **Setup and Troubleshooting Module**: Scripts and helpers for environment configuration, including PATH setup for .NET tools and diagnostic commands (e.g., clearing NuGet cache).

### Modules and Layers
- **Presentation Layer**: Command-line interface via npm scripts (e.g., `npm test` invokes the Test Runner). Handles user input/output, including error messages and progress indicators.
- **Business Logic Layer**: Contains tutorial modules (e.g., Core Concepts, Transactions, Many-to-Many Relationships), each with failing test logic that enforces EF pitfalls. Implements decision flows for entity state changes, transaction nesting, and query optimization.
- **Data Access Layer**: EF Core DbContext subclasses with entity models (e.g., Blog, Post). Manages migrations and provider-specific behaviors, enforcing constraints in SQLite mode.
- **Testing Layer**: Overlays the business logic, ensuring all tests start failing (e.g., by omitting SaveChanges() calls). Integrates with the assertion library for validation.
- **Infrastructure Layer**: Handles external integrations (e.g., .NET CLI for `dotnet-ef`), file I/O for SQLite databases, and npm-based orchestration.

The architecture supports modularity, allowing instructors to add new test modules without altering core components. Communication between layers uses dependency injection (e.g., for DbContext scoping), with no direct database access from the presentation layer.

## API Contracts
The design defines clear interfaces for inter-component interactions, focusing on .NET conventions for contracts.

### Interfaces
- **ITestRunner**: Interface for executing tests. Methods: `RunAllTests()`, `RunTestsByPattern(string pattern)`, `GetTestResults()`. Returns a list of `TestResult` objects containing pass/fail status, error messages, and fix explanations.
- **IAssertionLibrary**: Core assertion contract. Methods: `AssertEqual<T>(T expected, T actual, string message)`, `AssertThrows<TException>(Action action, string message)`, `AssertEFState(EntityState expected, EntityState actual)`. Throws custom `TutorialAssertionException` for educational feedback.
- **IDbProviderSwitcher**: Abstraction for database providers. Methods: `SwitchToInMemory()`, `SwitchToSQLite(string dbPath)`, `GetCurrentProvider()`. Integrates with EF Core's `DbContextOptionsBuilder`.
- **IDocumentationGenerator**: For generating docs. Methods: `GenerateDocs(IEnumerable<TestResult> results)`, outputting to a markdown file with sections per module.
- **IEFContext**: Base interface for DbContext subclasses. Methods: `AddEntity(object entity)`, `SaveChanges()`, `Query<T>() where T : class`. Extends `DbContext` for provider-specific overrides.

### Function Signatures
- **Test Execution**: `void RunFailingTest(Action testAction, string explanation)` – Wraps a test in failure logic, calling assertions and capturing exceptions.
- **Entity Operations**: `void TrackEntity<T>(T entity, EntityState state)` – Manages EF tracking, with validations for state mismatches.
- **Transaction Handling**: `void ExecuteInTransaction(Action action, bool nested = false)` – Wraps actions in `IDbContextTransaction`, enforcing atomicity and rollback on failure.
- **Query Operations**: `IQueryable<T> NoTrackingQuery<T>()` – Returns queries with `.AsNoTracking()`, highlighting modification pitfalls.

### Data Structures
- **TestResult**: Struct with properties: `bool Passed`, `string ErrorMessage`, `string FixExplanation`, `string ModuleName`.
- **EntityModel**: Base class for tutorial entities (e.g., Blog with Posts), implementing `IEntity` for state tracking.
- **ProviderConfig**: Enum-based config: `InMemory`, `SQLite`; includes connection strings for SQLite.
- **TutorialModule**: Class representing a module (e.g., Transactions), containing a list of `ITestRunner`-executable tests.

## Data Model
The data model is educational and sample-based, using EF Core Code-First approach with migrations for SQLite. It focuses on common relational patterns to illustrate pitfalls.

### Database Schema
- **Blogs Table**: Primary key `Id (int)`, `Title (string)`, `CreatedAt (DateTime)`. Represents a simple entity for core concepts.
- **Posts Table**: Primary key `Id (int)`, `Title (string)`, `Content (string)`, `BlogId (int, FK to Blogs)`. Enforces one-to-many relationships, with cascade deletes in SQLite.
- **Tags Table**: Primary key `Id (int)`, `Name (string)`. For many-to-many relationships.
- **PostTags Table (Junction)**: Composite key `PostId (int, FK)`, `TagId (int, FK)`. Demonstrates explicit vs. implicit many-to-many setups.
- **Migrations**: Handled via `dotnet-ef`, with initial migration for schema setup. InMemory mode ignores schema constraints.

### Data Structures and Relationships
- **Entity Classes**: `Blog` (has `ICollection<Post>`), `Post` (has `Blog` navigation), `Tag` (has `ICollection<Post>` via junction). Uses fluent API in `OnModelCreating` for relationships (e.g., `HasMany(b => b.Posts).WithOne(p => p.Blog)`).
- **Relationships**: One-to-many (Blog-Post), many-to-many (Post-Tag via PostTags). Highlights tracking issues (e.g., detached entities) and query behaviors (e.g., N+1 problems in future topics).
- **Constraints**: Foreign keys and cascades enforced in SQLite; absent in InMemory to demonstrate differences. Data seeding via `OnModelCreating` for failing test scenarios (e.g., orphaned posts).

The model is lightweight, with no complex inheritance or views, ensuring focus on EF Core fundamentals.

## Algorithms & Logic
Key algorithms implement the failing-first pedagogy and EF Core logic.

### Key Algorithms
- **Failing-First Test Execution**: For each test: Execute action → Catch exceptions via `AssertThrows` → If no exception or incorrect state, fail with educational message → User fixes → Re-run to pass. Logic ensures initial failure by omitting required calls (e.g., no `SaveChanges()`).
- **Change Detection Algorithm**: Monitors `DbContext.ChangeTracker` for entity states. Decision flow: Check `Entry(entity).State` → If `Unchanged` but modified, fail test → Fix by calling `Update()` or `Attach()`.
- **Transaction Management**: Nested transaction logic: Start outer transaction → Execute sub-actions → If failure, rollback outer → Enforce atomicity via `Commit()`. Handles concurrency via `IsolationLevel`.
- **Query Optimization**: For no-tracking queries: Apply `.AsNoTracking()` → Attempt modifications → Fail if tracked, teaching detachment. Future: Detect N+1 via query count analysis.

### Decision Flows and Business Logic
- **Provider Switching Flow**: User invokes switch → Update `DbContextOptions` → Recreate context → Run tests to observe constraint differences (e.g., FK violations in SQLite).
- **Troubleshooting Logic**: On failure (e.g., "Tool not found"): Check PATH → Suggest `./npm-dotnet.sh` wrapper → Reload environment. Integrates with npm scripts for diagnostics.
- **Documentation Generation**: Parse `[Explanation]` attributes from test methods → Aggregate by module → Output markdown with code snippets and fix steps.
- **Concurrency Handling (Advanced)**: Simulate conflicts via parallel contexts → Detect `DbUpdateConcurrencyException` → Implement resolution logic (e.g., reload and retry).

Business logic emphasizes EF Core best practices: Always check state before saving, use transactions for multi-entity ops, avoid N+1 with eager loading.

## Dependencies
- **Third-Party Libraries**: Entity Framework Core (Microsoft.EntityFrameworkCore, version 8.0+), Microsoft.EntityFrameworkCore.InMemory, Microsoft.EntityFrameworkCore.Sqlite. Custom assertion library as internal NuGet package.
- **External Services**: .NET CLI (for `dotnet-ef` migrations), npm (for scripting), SQLite runtime (bundled or system-installed).
- **Platform Dependencies**: .NET SDK 8.0, Node.js for npm. No cloud services; all local.
- **Version Constraints**: EF Core aligned with .NET 8.0 LTS; npm scripts assume standard versions.

## Questions & Clarifications
[AI-CLARIFY: Should the custom assertion library be a separate NuGet package or embedded in the project? For provider switching, is runtime reconfiguration sufficient, or does it require app restart? Confirm if all 33 tests need unique DbContext instances to avoid tracking conflicts. For advanced topics like concurrency, specify if simulated conflicts should use real threading or mock exceptions. Finally, ensure the documentation generator handles internationalization or remains English-only.]

## Cross-References
[Leave empty - references are documented in the metadata header above]

## AI Interaction Log
<!-- Auto-maintained by PromptPress extension -->