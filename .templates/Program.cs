using EFLab.Testing;

namespace EFLab;

/// <summary>
/// Main entry point for the Entity Framework tutorial test suite.
/// This is a "failing-first" tutorial - tests intentionally fail to teach EF pitfalls!
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        var runner = new TestRunner();

        // Parse command line arguments
        string? pattern = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--test" && i + 1 < args.Length)
            {
                pattern = args[i + 1];
                break;
            }
        }

        // Register all test modules
        RegisterCoreConceptTests(runner);
        RegisterInMemoryTests(runner);
        RegisterTransactionTests(runner);
        RegisterMultipleContextTests(runner);
        RegisterNoTrackingTests(runner);
        RegisterManyToManyTests(runner);

        // Run tests
        return runner.Run(pattern);
    }

    static void RegisterCoreConceptTests(TestRunner runner)
    {
        runner.RegisterTest(
            "DbContext_Lifecycle_TracksEntities",
            "CoreConcepts",
            () =>
            {
                // TODO: First test will go here
                Assert.IsTrue(false, "Not implemented yet - this is intentional!");
            }
        );
    }

    static void RegisterInMemoryTests(TestRunner runner)
    {
        runner.RegisterTest(
            "InMemory_Setup_Example",
            "InMemory",
            () =>
            {
                // TODO: In-memory DB tests
                Assert.IsTrue(true, "Placeholder");
            }
        );
    }

    static void RegisterTransactionTests(TestRunner runner)
    {
        runner.RegisterTest(
            "Transaction_Usage_Example",
            "Transactions",
            () =>
            {
                // TODO: Transaction tests
                Assert.IsTrue(true, "Placeholder");
            }
        );
    }

    static void RegisterMultipleContextTests(TestRunner runner)
    {
        runner.RegisterTest(
            "MultipleContexts_Tracking_Conflicts",
            "MultipleContexts",
            () =>
            {
                // TODO: Multiple context tests
                Assert.IsTrue(true, "Placeholder");
            }
        );
    }

    static void RegisterNoTrackingTests(TestRunner runner)
    {
        runner.RegisterTest(
            "NoTracking_Query_Example",
            "NoTracking",
            () =>
            {
                // TODO: No-tracking tests
                Assert.IsTrue(true, "Placeholder");
            }
        );
    }

    static void RegisterManyToManyTests(TestRunner runner)
    {
        runner.RegisterTest(
            "ManyToMany_Implicit_Setup",
            "ManyToMany",
            () =>
            {
                // TODO: Many-to-many tests
                Assert.IsTrue(true, "Placeholder");
            }
        );
    }
}
