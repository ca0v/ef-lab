using System.Reflection;
using EFLab.Testing;
using EFLab.Tests;

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
        bool generateDocs = false;
        string? providerArg = null;
        
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--test" && i + 1 < args.Length)
            {
                pattern = args[i + 1];
            }
            else if (args[i] == "--generate-docs")
            {
                generateDocs = true;
            }
            else if (args[i] == "--provider" && i + 1 < args.Length)
            {
                providerArg = args[i + 1].ToLower();
            }
        }

        // Set database provider
        if (providerArg == "sqlite")
        {
            DatabaseProvider.SetProvider(DatabaseProvider.Provider.SQLite);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Using SQLite provider for tests\n");
            Console.ResetColor();
        }
        else
        {
            DatabaseProvider.SetProvider(DatabaseProvider.Provider.InMemory);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Using InMemory provider for tests\n");
            Console.ResetColor();
        }

        // Auto-discover and register all tests with [Tutorial] attribute
        DiscoverAndRegisterTests(runner);

        // Generate documentation if requested
        if (generateDocs)
        {
            GenerateDocumentation();
            return 0;
        }

        // Run tests
        return runner.Run(pattern);
    }

    static void DiscoverAndRegisterTests(TestRunner runner)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var testClasses = assembly.GetTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("Tests"));

        foreach (var testClass in testClasses)
        {
            var methods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<TutorialAttribute>() != null)
                .OrderBy(m => m.GetCustomAttribute<TutorialAttribute>()!.Order);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<TutorialAttribute>()!;
                runner.RegisterTest(
                    attr.Title,
                    attr.Category,
                    () => method.Invoke(null, null)
                );
            }
        }
    }

    static void GenerateDocumentation()
    {
        Console.WriteLine("Generating tutorial documentation from test attributes...\n");

        var assembly = Assembly.GetExecutingAssembly();
        var testClasses = assembly.GetTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("Tests"))
            .OrderBy(t => t.Name);

        var docPath = Path.Combine(Directory.GetCurrentDirectory(), "TUTORIAL.md");
        using var writer = new StreamWriter(docPath);

        writer.WriteLine("# Entity Framework Core Tutorial");
        writer.WriteLine();
        writer.WriteLine("A failing-first approach to learning EF Core. Each test intentionally fails to teach you common pitfalls and how to fix them.");
        writer.WriteLine();

        foreach (var testClass in testClasses)
        {
            var methods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<TutorialAttribute>() != null)
                .OrderBy(m => m.GetCustomAttribute<TutorialAttribute>()!.Order);

            if (!methods.Any()) continue;

            writer.WriteLine($"# {testClass.Name.Replace("Tests", " Tests")}");
            writer.WriteLine();

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<TutorialAttribute>()!;
                writer.WriteLine(attr.ToMarkdown());
                writer.WriteLine("---");
                writer.WriteLine();
            }
        }

        Console.WriteLine($"Documentation generated: {docPath}");
    }
}
