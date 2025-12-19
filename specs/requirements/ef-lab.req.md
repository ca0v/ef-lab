---
artifact: ef-lab
phase: requirement
depends-on: []
references: []
version: 1.0.0
last-updated: 2025-12-19
---

# EF Core Failing-First Tutorial - Requirements

## Overview
This requirement specification defines the functional and non-functional needs for the EF Core Failing-First Tutorial, a hands-on learning lab designed to teach developers Entity Framework Core (EF Core) concepts through intentionally failing unit tests. The tutorial employs a "failing-first" pedagogy where all examples begin in a broken state, requiring learners to analyze errors, implement fixes, and understand EF Core pitfalls such as entity state management, relationships, transactions, and query behaviors. It utilizes a custom assertion library for feedback (excluding xUnit/MSTest), supports in-memory database testing for rapid iteration, and includes optional SQLite integration for enforcing realistic constraints. The system is structured into modules covering core concepts, in-memory limitations, transactions, multiple contexts, no-tracking queries, many-to-many relationships, troubleshooting, and advanced topics like concurrency and migrations. Operational workflows include project setup, test execution via npm scripts, database provider switching, troubleshooting failures, and documentation generation. The tutorial operates in local development environments with .NET SDK 8.0 or later, targeting .NET developers for educational purposes only, with no support for production deployments.

## Functional Requirements
- FR-1: The system shall provide a project setup mechanism that allows users to install dependencies, including .NET SDK 8.0, via npm scripts (e.g., `npm run setup`), configure environment PATH variables for cross-platform compatibility (Windows, macOS, Linux), and troubleshoot common setup issues such as NuGet cache corruption or missing tools, as referenced in @setup.req and SETUP.md.
- FR-2: The system shall support running failing unit tests using a custom assertion library, with all tests starting in a broken state to enforce learning through error correction, and provide feedback on EF Core operations without relying on external testing frameworks like xUnit or MSTest.
- FR-3: The system shall include a tutorial structure divided into modules (e.g., Core Concepts, Transactions, Many-to-Many Relationships), each with intentionally failing tests that cover specific EF Core topics, as outlined in tutorial_outline.req.md and tutorial_tests.req.md.
- FR-4: The system shall allow switching between database providers, defaulting to EF Core InMemory for unconstrained, fast learning and optionally supporting SQLite for enforcing relational constraints (e.g., foreign keys, transactions), with workflows for provider switching documented in database_providers.req.md.
- FR-5: The system shall provide troubleshooting capabilities for common operational failures, such as environment-specific exceptions (e.g., "Settings file not found"), including helper scripts (e.g., `./npm-dotnet.sh test`) and guidance to reload environments or clear caches, as integrated into SETUP.md.
- FR-6: The system shall enable documentation generation from test attributes using npm scripts (e.g., `npm run docs:generate`), producing TUTORIAL.md with explanations of fixes and best practices for each test scenario.
- FR-7: The system shall support advanced learning scenarios, including pattern-specific test execution (e.g., `npm run test:pattern -- "Transaction"`), handling of nested transactions, atomicity, and topics like concurrency, migrations, and DI scoping, with future topics (e.g., N+1 Query Problem, Projections) prepared in future_topics.req.md.
- FR-8: The system shall define user roles, with Learners (developers) responsible for running tests, fixing failures, and generating docs, and Instructors/Contributors for extending tests and maintaining the custom assertion library, as inferred from operational workflows.
- FR-9: The system shall integrate with external tools, including EF Core DbContext, .NET CLI for `dotnet-ef` commands (e.g., for migrations), and database providers, ensuring CLI-based interactions via npm scripts for all operations.
- FR-10: The system shall enforce failing-first pedagogy, where tests only pass after user-implemented fixes, with no allowance for partial success in scenarios like transactions or relationship handling.

## Non-Functional Requirements
- NFR-1: Performance - The system shall support fast test execution in InMemory mode for basic learning (sub-second feedback), while SQLite mode may introduce minor overhead (up to 5-10 seconds per test run) to simulate real-world constraints, ensuring responsiveness in local development environments.
- NFR-2: Scalability - The system shall operate on standard developer hardware with .NET SDK 8.0, handling up to 33 tests (as per tutorial_tests.req.md) without requiring distributed resources, with memory usage capped at typical .NET application levels for in-memory databases.
- NFR-3: Security - The system shall not handle production data or expose sensitive information, operating entirely locally with no network dependencies, and shall avoid any code that could lead to security vulnerabilities in educational contexts.
- NFR-4: Usability - The system shall provide clear, command-line based interactions via npm scripts, with error messages guiding users to fixes, assuming basic .NET knowledge, and supporting multilingual environments through platform-agnostic tools.
- NFR-5: Reliability - The system shall ensure consistent behavior across supported platforms (Windows, macOS, Linux), with built-in troubleshooting for environment issues, and guarantee that failing tests accurately reflect EF Core pitfalls without false positives in InMemory mode.
- NFR-6: Maintainability - The system shall use modular code structure for tests and assertions, allowing instructors to add new tests or modules easily, with documentation generated automatically from test attributes to facilitate updates.
- NFR-7: Portability - The system shall be platform-independent, relying on .NET Core's cross-platform capabilities and npm for scripting, with no hardcoded paths beyond standard PATH configurations.
- NFR-8: Constraints Enforcement - The system shall strictly enforce database provider limitations (e.g., no foreign key enforcement in InMemory), using failing tests to highlight differences, and limit scope to educational use without production applicability.

## Questions & Clarifications
[AI-CLARIFY: The ConOps assumes learners have basic .NET knowledge, but it would be helpful to clarify the exact prerequisite skill level (e.g., familiarity with C# syntax vs. full EF experience). Additionally, the number of tests is specified as 33, but without the full tutorial_tests.req.md, confirmation on whether this includes all modules or only current ones is needed. The role of Instructors/Contributors is implied but not detailed—clarify if they have specific permissions or tools for editing. Finally, performance metrics (e.g., exact timings for SQLite vs. InMemory) are estimated; specify if benchmarks are required.]

## Cross-References
[Leave empty - references are documented in the metadata header]

## AI Interaction Log
<!-- Auto-maintained by PromptPress extension -->