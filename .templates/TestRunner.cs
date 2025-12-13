namespace EFLab.Testing;

/// <summary>
/// Simple test runner for Entity Framework tutorial.
/// Runs tests with pattern matching support.
/// </summary>
public class TestRunner
{
    private readonly List<TestCase> _tests = new();
    private int _passed = 0;
    private int _failed = 0;
    private int _skipped = 0;

    public void RegisterTest(string name, string category, Action testAction)
    {
        _tests.Add(new TestCase(name, category, testAction));
    }

    public int Run(string? pattern = null)
    {
        Console.WriteLine("=== Entity Framework Tutorial - Test Runner ===\n");

        var testsToRun = string.IsNullOrEmpty(pattern)
            ? _tests
            : _tests.Where(t => t.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                               t.Category.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!testsToRun.Any())
        {
            Console.WriteLine($"No tests found matching pattern: {pattern}");
            return 0;
        }

        Console.WriteLine($"Running {testsToRun.Count} tests...\n");

        foreach (var test in testsToRun)
        {
            RunTest(test);
        }

        PrintSummary();
        return _failed > 0 ? 1 : 0;
    }

    private void RunTest(TestCase test)
    {
        Console.Write($"[{test.Category}] {test.Name}... ");

        try
        {
            test.Action();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ PASSED");
            Console.ResetColor();
            _passed++;
        }
        catch (AssertionException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ FAILED");
            Console.ResetColor();
            Console.WriteLine($"  {ex.Message}\n");
            _failed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ ERROR");
            Console.ResetColor();
            Console.WriteLine($"  Unexpected error: {ex.GetType().Name}");
            Console.WriteLine($"  {ex.Message}");
            Console.WriteLine($"  {ex.StackTrace}\n");
            _failed++;
        }
    }

    private void PrintSummary()
    {
        Console.WriteLine("\n=== Test Summary ===");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Passed: {_passed}");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Failed: {_failed}");
        Console.ResetColor();

        if (_skipped > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Skipped: {_skipped}");
            Console.ResetColor();
        }

        Console.WriteLine($"Total: {_tests.Count}");
        Console.WriteLine("====================\n");
    }

    private record TestCase(string Name, string Category, Action Action);
}
