# Concept of Operations (ConOps)

## Purpose and Scope
The Entity Framework (EF) Core Failing-First Tutorial is an interactive, hands-on learning system designed to teach developers the fundamentals and common pitfalls of EF Core through intentionally failing unit tests. The system focuses on a "failing-first" approach where all examples start broken, requiring learners to identify, understand, and fix issues related to EF Core operations such as entity state management, relationships, transactions, and query behaviors. The tutorial uses a custom assertion library (excluding xUnit/MSTest) to maintain focus on EF Core concepts and supports in-memory database testing for rapid feedback, with optional SQLite integration for realistic constraint enforcement. Operational boundaries include development environments running .NET SDK 8.0 or later, with support for Windows, macOS, and Linux. The scope excludes production deployments or real-world application builds; it is strictly educational, emphasizing learning through error correction and documentation generation from test attributes.

## Operational Environment
The tutorial operates in a local development environment, typically an IDE (e.g., Visual Studio Code or Visual Studio) with .NET SDK 8.0 installed. Learners interact via command-line interfaces using npm scripts for setup, testing, and documentation generation. The system supports two database providers: EF Core InMemory (default for fast, unconstrained learning) and SQLite (for enforcing relational constraints like foreign keys and transactions). Environments must include Node.js for npm script execution and .NET tools for EF Core operations. Operational constraints include platform-specific PATH configurations (e.g., adding .NET to ~/.bashrc on Linux) and database file creation for SQLite. The environment is not suited for distributed or cloud-based setups; all operations are local and iterative.

## User Roles and Responsibilities
- **Learner/Developer**: The primary user, typically a .NET developer learning EF Core. Responsibilities include installing dependencies (e.g., .NET SDK via npm scripts), setting up the project, running failing tests, analyzing errors, implementing fixes, and generating documentation. Learners must understand EF Core pitfalls to progress through modules.
- **Instructor/Contributor** (Implied): Secondary role for those extending the tutorial. Responsibilities include adding new tests, updating documentation, and maintaining the custom assertion library. (Inferred from the tutorial's extensible structure.)
Users are expected to have basic .NET knowledge; no advanced roles are defined.

## Operational Scenarios
Key use cases and workflows include:
1. **Project Setup**: User installs .NET SDK 8.0, runs `npm run setup`, and configures PATH for .NET tools. Workflow: Follow Quick Start steps, troubleshoot via SETUP.md if issues arise (e.g., clearing NuGet cache).
2. **Running Failing Tests**: User executes `npm test` for InMemory tests or `npm run test:sqlite` for SQLite, observing failures. Workflow: Identify pitfalls (e.g., forgetting SaveChanges), fix code, re-run tests, and verify passes.
3. **Switching Database Providers**: User switches to SQLite for realistic constraints. Workflow: Run SQLite-specific commands, observe differences (e.g., foreign key enforcement), and apply fixes.
4. **Troubleshooting Failures**: User encounters exceptions (e.g., "Settings file not found"). Workflow: Follow Troubleshooting section, use helper scripts like `./npm-dotnet.sh test`, and reload environment.
5. **Generating Documentation**: User runs `npm run docs:generate` to create TUTORIAL.md from test attributes. Workflow: Execute post-fix, review generated docs for explanations.
6. **Advanced Learning**: User explores modules like transactions or many-to-many relationships. Workflow: Run pattern-specific tests (e.g., `npm run test:pattern -- "Transaction"`), fix nested transaction issues, and understand atomicity.

These scenarios emphasize iterative learning: fail → analyze → fix → succeed.

## System Interfaces
- **User Interfaces**: Command-line via npm scripts (e.g., `npm test`, `npm run install:ef`) and helper scripts (e.g., `./npm-dotnet.sh`). Outputs include test results, error messages, and generated docs. Interactions are text-based, with no GUI.
- **System-to-System Interfaces**: Integrates with EF Core (via DbContext and providers), .NET CLI (for `dotnet-ef` commands), and SQLite/InMemory databases. npm orchestrates .NET operations; custom assertion library provides feedback without external testing frameworks.
- **External Tools**: Relies on `dotnet-ef` for migrations/modeling (e.g., Model First vs. Database First), with CLI flags for pattern matching.

## Operational Constraints
- Database provider limitations: InMemory does not enforce foreign keys, transactions, or cascade deletes, leading to false positives in tests. SQLite introduces performance overhead but ensures realism.
- Environment requirements: Requires .NET SDK 8.0; failures occur if PATH is misconfigured or NuGet cache is corrupted.
- Test design: All tests fail initially; users must fix before progression. No partial success allowed in transactional scenarios.
- Performance: InMemory is fast for basics; SQLite is slower but necessary for advanced topics like N+1 queries.
- Security/Safety: No production data handling; operations are local and educational.

## Requirements Traceability
This ConOps traces to the following requirement overviews, describing what operations each addresses:
- **SETUP.md** (Installation and usage): Addresses setup workflows, environment configuration, and troubleshooting for initial project operation.
- **DATABASE_PROVIDERS.md** (Provider comparison): Covers switching between InMemory and SQLite, including pros/cons and constraint enforcement for realistic testing scenarios.
- **TUTORIAL.md** (Generated docs): Describes documentation generation and detailed test explanations for post-fix learning operations.
- **specs/requirements/@ef-lab.req** (Functional and non-functional requirements): Outlines tutorial structure, modules, failing-first approach, user roles, and operational constraints.

This ConOps ensures operational focus, describing practical use in a development context while aligning with the tutorial's educational goals.