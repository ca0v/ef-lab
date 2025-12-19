---
artifact: ef-lab
phase: implementation
depends-on: []
references: ["ef-lab.req", "ef-lab.design"]
version: 1.0.0
last-updated: 2025-12-19
---

# Ef Lab - Implementation

## Overview
The EF Core Failing-First Tutorial (Ef Lab) is implemented as a .NET 8.0 console application with a modular architecture, utilizing EF Core for database operations, a custom assertion library for test feedback, and npm scripts for cross-platform orchestration. The implementation enforces a failing-first pedagogy where tests start broken, requiring learners to fix EF Core-related issues. It supports InMemory (default) and SQLite providers, includes diagnostic helpers for troubleshooting (e.g., PATH checks, exception logs), and generates documentation from tests. Key features include modular tutorial structure, provider switching via configuration, and CLI-based interactions. The system is designed for local development, ensuring fast iterations with InMemory and realistic constraints with SQLite, while integrating enhanced error diagnostics from the design updates.

## File Structure
```
ef-lab/
├── src/
│   ├── EfLab.csproj
│   ├── Program.cs
│   ├── Components/
│   │   ├── SetupComponent.cs
│   │   ├── AssertionLibrary.cs
│   │   ├── TutorialModule.cs
│   │   ├── DatabaseSwitcher.cs
│   │   ├── TestRunner.cs
│   │   └── EfContextManager.cs
│   ├── DataStructures/
│   │   ├── TestCase.cs
│   │   ├── AssertionResult.cs
│   │   ├── DbContextConfiguration.cs
│   │   └── TutorialModule.cs
│   ├── Interfaces/
│   │   ├── ISetupApi.cs
│   │   ├── IAssertionApi.cs
│   │   ├── ITutorialApi.cs
│   │   ├── IDatabaseApi.cs
│   │   └── ITestRunnerApi.cs
│   └── Utils/
│       └── DiagnosticsHelper.cs
├── tests/
│   ├── EfLab.Tests.csproj
│   ├── FailingTests/
│   │   ├── CoreConceptsTests.cs
│   │   ├── TransactionTests.cs
│   │   ├── ManyToManyTests.cs
│   │   └── AdvancedTests.cs
│   └── TestHelpers.cs
├── package.json
├── appsettings.json
├── SETUP.md
└── TUTORIAL.md
```

## Modules & Components
### SetupComponent
- Purpose: Manages project setup, dependency installation, PATH configuration, and diagnostics for common issues like NuGet cache corruption or missing .NET SDK.
- Exports: Public methods for installation and diagnostics via ISetupApi.
- Dependencies: .NET CLI, npm scripts, DiagnosticsHelper.

### AssertionLibrary
- Purpose: Provides custom assertions for EF Core operations, generating feedback and diagnostic logs for failing tests.
- Exports: Assertion methods for entity states, relationships, and transactions via IAssertionApi.
- Dependencies: EF Core, DiagnosticsHelper for logging.

### TutorialModule
- Purpose: Structures tutorial modules with failing tests, troubleshooting guides, and execution logic.
- Exports: Methods to load and execute module tests via ITutorialApi.
- Dependencies: TestRunner, AssertionLibrary, EfContextManager.

### DatabaseSwitcher
- Purpose: Handles switching between InMemory and SQLite providers, validating constraints, and generating diagnostic reports.
- Exports: Provider switching and validation methods via IDatabaseApi.
- Dependencies: EF Core providers, DbContextConfiguration, DiagnosticsHelper.

### TestRunner
- Purpose: Executes failing tests, analyzes failures, and integrates diagnostics for troubleshooting.
- Exports: Test execution and failure analysis methods via ITestRunnerApi.
- Dependencies: TutorialModule, AssertionLibrary, DiagnosticsHelper.

### EfContextManager
- Purpose: Manages EF Core DbContext instances, entity states, relationships, and queries across providers.
- Exports: Context creation, state management, and query methods.
- Dependencies: EF Core, DbContextConfiguration.

## Implementation Details
### SetupComponent.InstallDependencies
- Signature: `Task InstallDependencies()`
- Logic: 
  1. Check for .NET SDK 8.0+ using `dotnet --version`.
  2. If not found, prompt user to install and exit.
  3. Run `dotnet restore` to install NuGet packages.
  4. Use DiagnosticsHelper to log any NuGet cache issues and clear if necessary.
  5. Return success or diagnostic report.
- Edge cases: Handle missing SDK by logging error and providing download link; catch exceptions from `dotnet restore` and trigger cache clear script.

### AssertionLibrary.AssertEntityState
- Signature: `AssertionResult AssertEntityState<TEntity>(TEntity entity, EntityState expectedState)`
- Logic: 
  1. Retrieve actual state from DbContext.Entry(entity).State.
  2. Compare with expectedState.
  3. If mismatch, generate feedback string and log diagnostic details (e.g., entity ID, context state).
  4. Return AssertionResult with IsPassed, Feedback, and ErrorDetails.
- Edge cases: Validate entity is tracked; handle detached entities by logging "Entity not tracked" and suggesting Attach().

### TutorialModule.LoadModule
- Signature: `TutorialModule LoadModule(string moduleName)`
- Logic: 
  1. Load module metadata from embedded resources or config.
  2. Instantiate TutorialModule with tests and troubleshooting guide.
  3. Use DiagnosticsHelper to check for environment-specific exceptions (e.g., "Settings file not found").
  4. Return the module or throw with diagnostic log.
- Edge cases: If module not found, log "Module not available" and suggest available modules; handle config file corruption by reloading defaults.

### DatabaseSwitcher.SwitchProvider
- Signature: `Task SwitchProvider(ProviderType providerType)`
- Logic: 
  1. Update DbContextConfiguration with new providerType and connection string.
  2. Rebuild DbContext options with the new provider.
  3. Validate constraints (e.g., test foreign key enforcement in SQLite).
  4. Generate diagnostic report if switching fails.
  5. Return updated configuration.
- Edge cases: Handle invalid connection strings by logging and reverting; catch provider-specific exceptions (e.g., SQLite file access denied).

### TestRunner.RunAllTests
- Signature: `Task<IEnumerable<AssertionResult>> RunAllTests(bool diagnosticMode = false)`
- Logic: 
  1. Iterate through all modules and their tests.
  2. For each test, execute via TutorialModule.ExecuteTests.
  3. If diagnosticMode, enable DiagnosticsHelper for detailed logs.
  4. Collect AssertionResults and aggregate failures.
  5. Return results with optional diagnostic summaries.
- Edge cases: Handle test timeouts by logging and skipping; detect infinite loops in failing tests and abort with warning.

### EfContextManager.CreateContext
- Signature: `DbContext CreateContext(DbContextConfiguration config)`
- Logic: 
  1. Based on config.ProviderType, configure options (InMemory or SQLite).
  2. Add connection string for SQLite.
  3. Enable sensitive data logging in debug mode.
  4. Instantiate and return DbContext.
- Edge cases: Validate config; handle SQLite file not found by logging and creating if needed; ensure InMemory doesn't enforce constraints.

## Data Structures
```csharp
public class TestCase
{
    public string ModuleName { get; set; }
    public string Description { get; set; }
    public string ExpectedFailure { get; set; }
    public List<string> DiagnosticLogs { get; set; } = new();
}

public class AssertionResult
{
    public bool IsPassed { get; set; }
    public string Feedback { get; set; }
    public Dictionary<string, object> ErrorDetails { get; set; } = new();
}

public class DbContextConfiguration
{
    public enum ProviderType { InMemory, SQLite }
    public ProviderType ProviderType { get; set; }
    public string ConnectionString { get; set; }
    public List<DiagnosticEntry> Diagnostics { get; set; } = new();

    public class DiagnosticEntry
    {
        public string Issue { get; set; }
        public string Resolution { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

public class TutorialModule
{
    public string Name { get; set; }
    public List<TestCase> Tests { get; set; } = new();
    public Dictionary<string, string> TroubleshootingGuide { get; set; } = new();
}
```

## Test Scenarios
- Test 1: Run CoreConceptsTests in InMemory mode - Expect all tests to fail initially with feedback on entity state mismatches; after fixes (e.g., Attach entity), tests pass.
- Test 2: Switch to SQLite and run TransactionTests - Expect failures due to uncommitted transactions; diagnostics log foreign key violations; after wrapping in TransactionScope, tests pass.
- Test 3: Execute ManyToManyTests with diagnostic mode - Expect relationship failures; logs include entity IDs and state; fixes involve proper junction table handling.
- Test 4: Simulate "Settings file not found" in SetupComponent - Expect diagnostic logs suggesting file recreation; helper script reloads environment.
- Test 5: Run AdvancedTests for concurrency - Expect optimistic concurrency exceptions; diagnostics provide SQL logs; fixes use row versions.
- Edge case: Provider switch failure due to invalid SQLite path - Expect diagnostic report with resolution to update connection string; revert to InMemory.
- Edge case: Test timeout in long-running query - Expect skip with warning log; user fixes by optimizing query.

## Dependencies
- Microsoft.EntityFrameworkCore: Core EF Core library for ORM operations.
- Microsoft.EntityFrameworkCore.InMemory: For fast, unconstrained testing.
- Microsoft.EntityFrameworkCore.Sqlite: For realistic constraints like foreign keys.
- Microsoft.Extensions.Configuration: For loading appsettings.json.
- npm: For scripting setup and test execution (via package.json scripts like "npm run setup" calling dotnet CLI).

## Configuration
Environment variables: DOTNET_ROOT (for SDK path), EF_LAB_PROVIDER (to set default provider: InMemory or SQLite). Config files: appsettings.json for connection strings and module settings. Setup: Run `npm run setup` to install dependencies and configure PATH; diagnostics via DiagnosticsHelper check for issues on startup.

## Deployment Notes
Build process: Use `dotnet build` for the main project and tests. Deployment: Local only; no production support. Run via npm scripts (e.g., `npm run test` executes `dotnet test` with custom runner). Ensure .NET SDK 8.0+ is installed; package as ZIP for distribution.

## Cross-References
[Leave empty - references are documented in the metadata header above]

## AI Interaction Log
<!-- Auto-maintained by PromptPress extension -->