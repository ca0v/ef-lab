---
artifact: ef-lab
phase: design
depends-on: ["ef-lab.req"]
references: []
version: 1.0.0
last-updated: 2025-12-19
---

# Ef Lab - Design

## Architecture Overview
The EF Core Failing-First Tutorial (Ef Lab) is designed as a modular, hands-on educational tool for .NET developers to learn EF Core concepts through intentional test failures. The updated requirements, particularly in the "Questions & Clarifications" section, emphasize enhanced troubleshooting for setup and operational failures (e.g., environment-specific exceptions like "Settings file not found"), with added lines clarifying helper scripts for diagnostics and cross-platform PATH configurations. This integrates into the architecture by expanding setup and error-handling components to provide more robust, user-friendly feedback and diagnostics.

The high-level architecture is a layered structure built on .NET 8.0+, utilizing npm for orchestration of setup and test execution. It includes:
- A **Presentation Layer** for user interaction via npm scripts and console output.
- A **Business Logic Layer** comprising EF Core contexts, custom assertions, and tutorial modules.
- A **Data Layer** supporting pluggable providers (EF Core InMemory by default, with optional SQLite).
- A **Testing Layer** with failing-first tests and custom assertion library for feedback.
- Cross-cutting concerns: Configuration management, error diagnostics, and provider switching.

This design ensures educational isolation (no production deployment), rapid iteration with in-memory DB, and realistic constraints via SQLite. The changes necessitate enhanced error-handling modules to address clarified troubleshooting needs, integrating seamlessly with existing npm-based workflows.

## Component Design
The system is decomposed into the following key components, updated to incorporate troubleshooting enhancements from the modified "Questions & Clarifications" section, which adds support for diagnostic helper scripts and environment-specific exception handling.

- **Project Setup Component**: Handles dependency installation (.NET SDK 8.0+) via npm scripts (e.g., `npm run setup`). Includes PATH configuration for cross-platform compatibility (Windows, macOS, Linux) and integration of new troubleshooting helpers (e.g., scripts to detect NuGet cache issues or missing tools, referencing @setup.req and SETUP.md). This component now includes a Diagnostics submodule for logging and resolving setup failures, such as PATH variable errors or SDK version mismatches.
  
- **Custom Assertion Library**: A lightweight library (excluding xUnit/MSTest) for test assertions and feedback on EF Core operations. It provides structured error messages for failing tests, now extended with diagnostic outputs for operational failures (e.g., "Settings file not found"), integrating the added lines on helper scripts to generate diagnostic logs.

- **Tutorial Module Structure**: Organized into modules (Core Concepts, Transactions, Many-to-Many Relationships, etc.) with failing tests. Each module uses a shared EF Core context and custom assertions. Updated to include troubleshooting workflows for module-specific failures, such as environment exceptions, with helper scripts to inspect database states or context configurations (referencing tutorial_outline.req.md and tutorial_tests.req.md).

- **Database Provider Switcher**: Manages switching between InMemory (default for fast, unconstrained learning) and SQLite (for relational constraints). Includes workflows for provider toggling via npm scripts, now enhanced with diagnostic checks for provider-specific issues (e.g., foreign key violations in SQLite), addressing the clarified troubleshooting needs (referencing database_providers.req.md).

- **Test Execution Engine**: Orchestrates running failing tests via npm scripts. Integrates with the custom assertion library for feedback. The updated design adds a Failure Analyzer submodule to parse and troubleshoot common operational failures, generating documentation-like outputs for error correction.

- **EF Core Context Manager**: Handles entity state, relationships, and queries. Supports multiple contexts and no-tracking modes. Extended with diagnostic logging for state management pitfalls, incorporating the new troubleshooting capabilities.

All components are packaged as a .NET class library or console app, with npm acting as the entry point for cross-platform execution. The changes ensure that diagnostic helpers are reusable across components, reducing redundancy and improving user experience.

## Data Structures
Key data structures are defined to support the tutorial's failing-first pedagogy and the updated troubleshooting features, which require structures for logging and diagnostic data.

- **TestCase Structure**: A class representing a tutorial test case.
  - Properties: `ModuleName` (string), `Description` (string), `ExpectedFailure` (string), `DiagnosticLogs` (list of strings - new field for troubleshooting, capturing helper script outputs like PATH checks or exception details).
  - Usage: Stores metadata for each failing test, now including logs for environment-specific failures.

- **AssertionResult Structure**: Extends the custom assertion library's output.
  - Properties: `IsPassed` (bool), `Feedback` (string), `ErrorDetails` (dictionary<string, object> - updated to include keys like "ExceptionType" for clarified operational failures, e.g., "Settings file not found").
  - Usage: Provides detailed feedback, integrating diagnostic data from helper scripts.

- **DbContextConfiguration Structure**: Manages provider settings.
  - Properties: `ProviderType` (enum: InMemory, SQLite), `ConnectionString` (string), `Diagnostics` (list of DiagnosticEntry - new nested structure with fields: `Issue` (string), `Resolution` (string), `Timestamp` (DateTime)).
  - Usage: Supports provider switching and troubleshooting, with diagnostics logging provider-specific issues.

- **TutorialModule Structure**: Represents a module (e.g., Core Concepts).
  - Properties: `Name` (string), `Tests` (list of TestCase), `TroubleshootingGuide` (dictionary<string, string> - new field mapping failure types to resolutions, incorporating added helper script references).
  - Usage: Structures module content, now with integrated troubleshooting for enhanced clarity.

These structures are implemented as C# classes with EF Core annotations where applicable, ensuring compatibility with both InMemory and SQLite providers.

## API Design
APIs are defined as internal interfaces for the .NET components, exposed via npm scripts for user interaction. Updated to include new endpoints for troubleshooting, integrating the modified requirements.

- **ISetupApi**: Interface for project setup.
  - Methods: `InstallDependencies()` (installs .NET SDK via npm), `ConfigurePath()` (cross-platform PATH setup), `RunDiagnostics()` (new method: executes helper scripts for issues like NuGet cache corruption, returning diagnostic logs).
  - Integration: Addresses FR-1, with added diagnostic capabilities.

- **IAssertionApi**: Interface for custom assertions.
  - Methods: `AssertEntityState(entity, expectedState)`, `ProvideFeedback(failureDetails)` (updated to accept diagnostic data, e.g., exception types).
  - Integration: Supports FR-2, enhanced for troubleshooting failures.

- **ITutorialApi**: Interface for module management.
  - Methods: `LoadModule(moduleName)`, `ExecuteTests(moduleName)` (now includes optional diagnostic mode), `GetTroubleshootingHelp(failureType)` (new method: returns resolutions from helper scripts).
  - Integration: Covers FR-3, with added troubleshooting.

- **IDatabaseApi**: Interface for provider management.
  - Methods: `SwitchProvider(providerType)`, `ValidateConstraints()` (checks for SQLite-specific issues), `GenerateDiagnosticReport()` (new method: produces reports on operational failures).
  - Integration: Aligns with FR-4, incorporating diagnostic workflows.

- **ITestRunnerApi**: Interface for execution.
  - Methods: `RunAllTests()`, `AnalyzeFailure(testCase)` (new method: uses helper scripts to dissect failures, e.g., environment exceptions).
  - Integration: Supports FR-5, with enhanced error handling.

All APIs are asynchronous where applicable, using .NET's Task-based model, and return structured results including diagnostic data.

## Performance Considerations
The design prioritizes educational efficiency, with InMemory provider enabling sub-second test iterations. SQLite adds overhead for realism (e.g., ~10-50ms per transaction due to disk I/O), mitigated by optional switching. The updated troubleshooting features (helper scripts and diagnostics) introduce minor overhead (~5-10ms per diagnostic run), optimized via lazy logging and caching of diagnostic results. Memory usage is low (under 100MB for typical runs), with EF Core's change tracking optimized for small datasets. Cross-platform npm execution ensures consistent performance, with considerations for CI/CD-like environments to avoid production scaling issues. Overall, the system remains lightweight, focusing on rapid feedback for learning.