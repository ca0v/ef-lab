# Table of Contents (TOC) for ConOps.md

This table lists all domain terms identified from the ConOps.md document, with cross-references to README.md and ef-lab.req.md for clarification where needed. Terms are sorted alphabetically for ease of reference.

| Term                          | Brief Definition/Description |
|-------------------------------|------------------------------|
| .NET CLI                     | Command-line interface for .NET operations, used for executing commands like `dotnet-ef` in the tutorial's development environment. |
| .NET SDK 8.0                 | Software Development Kit required for the tutorial, version 8.0 or later, enabling .NET Core development across supported platforms (Windows, macOS, Linux). |
| Advanced Learning            | Operational scenario involving exploration of complex topics like transactions or many-to-many relationships, with workflows for running pattern-specific tests and fixing issues. |
| Command-Line Interfaces      | Text-based interfaces for user interactions in the tutorial, including npm scripts and helper scripts like `./npm-dotnet.sh`, with no GUI support. |
| Custom Assertion Library     | A proprietary library used in the tutorial for providing test feedback on EF Core operations, excluding external frameworks like xUnit or MSTest to maintain focus on core concepts. |
| DATABASE_PROVIDERS.md        | Documentation file detailing database provider comparisons, including switching between InMemory and SQLite, pros/cons, and constraint enforcement. |
| DbContext                    | Core EF Core component for database interactions, used in the tutorial for entity state management, relationships, and operations like SaveChanges. |
| dotnet-ef                    | .NET CLI tool for Entity Framework operations, such as migrations and modeling, integrated via npm scripts in the tutorial. |
| Entity Framework (EF) Core   | Open-source ORM framework for .NET, central to the tutorial's focus on teaching fundamentals, pitfalls, and operations through failing tests. |
| Failing-First Approach       | Pedagogical method where all tutorial tests start broken, requiring learners to analyze, fix, and understand EF Core issues like entity state or transactions. |
| IDE                          | Integrated Development Environment, such as Visual Studio Code or Visual Studio, used for local development in the tutorial's operational environment. |
| In-Memory Database           | Default EF Core provider for fast, unconstrained testing in the tutorial, lacking enforcement of foreign keys or transactions but enabling rapid feedback. |
| Instructor/Contributor       | Secondary user role for extending the tutorial, involving responsibilities like adding tests, updating docs, and maintaining the custom assertion library. |
| Learner/Developer            | Primary user role, a .NET developer learning EF Core through installing dependencies, running failing tests, fixing issues, and generating documentation. |
| Many-to-Many Relationships   | EF Core relationship type covered in the tutorial, with failing tests on implicit/explicit setups, duplicates, and cascade behavior (e.g., Student-Course). |
| npm Scripts                  | Node.js package manager scripts used for tutorial operations, including setup, testing (e.g., `npm test`), and documentation generation (e.g., `npm run docs:generate`). |
| Operational Constraints     | Limitations in the tutorial, such as database provider differences (e.g., InMemory not enforcing foreign keys), environment requirements, and performance trade-offs. |
| Operational Environment     | Local development setup for the tutorial, requiring .NET SDK 8.0, Node.js, and tools like IDEs or CLIs, with support for Windows, macOS, and Linux. |
| Operational Scenarios       | Key use cases in the tutorial, including project setup, running failing tests, switching providers, troubleshooting, and generating docs, emphasizing iterative learning. |
| PATH Configurations          | Environment variable setups (e.g., adding .NET to ~/.bashrc on Linux) required for tool execution in the tutorial's operational environment. |
| Project Setup                | Initial workflow for installing .NET SDK 8.0, running `npm run setup`, and configuring PATH, with troubleshooting via SETUP.md. |
| Requirements Traceability    | Mapping of ConOps operations to requirement documents like SETUP.md, DATABASE_PROVIDERS.md, TUTORIAL.md, and ef-lab.req.md for alignment. |
| Running Failing Tests       | Core scenario where users execute tests (e.g., `npm test` for InMemory or `npm run test:sqlite` for SQLite), observe failures, and fix EF Core pitfalls. |
| SETUP.md                     | Documentation file covering installation, usage, environment configuration, and troubleshooting for project setup and initial operations. |
| SQLite                       | Relational database provider optionally used in the tutorial for enforcing constraints like foreign keys and transactions, creating .db files. |
| specs/requirements/@ef-lab.req | Requirement specification file outlining functional/non-functional needs, tutorial structure, failing-first pedagogy, user roles, and constraints. |
| Switching Database Providers | Workflow for toggling between InMemory (fast basics) and SQLite (realistic constraints), highlighting differences in enforcement and behavior. |
| System Interfaces           | Interaction points in the tutorial, including user interfaces (CLI-based), system-to-system (EF Core/DbContext integration), and external tools (e.g., .NET CLI). |
| System-to-System Interfaces | Integrations between tutorial components, such as EF Core with DbContext/providers, .NET CLI, and databases, orchestrated via npm. |
| Transactions                 | EF Core feature for atomic operations, covered in failing tests on commit/rollback, nesting, and sharing across contexts. |
| Troubleshooting Failures    | Scenario for resolving issues like "Settings file not found," using helper scripts, reloading environments, and clearing caches. |
| TUTORIAL.md                  | Auto-generated documentation from test attributes, providing explanations of fixes and best practices post-test execution. |
| Unit Tests                   | Failing tests in the tutorial that teach EF Core by starting broken, requiring fixes for persistence, relationships, and queries. |
| User Interfaces              | Command-line based interactions in the tutorial, including npm scripts and outputs like test results or generated docs. |
| User Roles and Responsibilities | Definitions of roles like Learner/Developer (fixing tests) and Instructor/Contributor (extending tutorial), with expectations of basic .NET knowledge. |

---

This TOC ensures completeness by covering all major domain terms from `ConOps.md` while incorporating clarifications from `README.md` (e.g., Quick Start steps, provider details) and `ef-lab.req.md` (e.g., functional requirements like FR-1 to FR-10). If discrepancies arise in future syncs (e.g., new terms added to ConOps.md), they can be noted in a "Discrepancies" section at the bottom as per the instructions. Let me know if further refinements are needed!