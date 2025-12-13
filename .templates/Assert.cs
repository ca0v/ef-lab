namespace EFLab.Testing;

/// <summary>
/// Custom assertion library for Entity Framework tests.
/// All tests start failing - you'll learn why and how to fix them!
/// </summary>
public static class Assert
{
    public static void IsTrue(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new AssertionException(message ?? "Expected true but was false");
        }
    }

    public static void IsFalse(bool condition, string? message = null)
    {
        if (condition)
        {
            throw new AssertionException(message ?? "Expected false but was true");
        }
    }

    public static void AreEqual<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertionException(
                message ?? $"Expected: {expected}\nActual: {actual}"
            );
        }
    }

    public static void AreNotEqual<T>(T expected, T actual, string? message = null)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertionException(
                message ?? $"Expected values to be different, but both were: {actual}"
            );
        }
    }

    public static void IsNull<T>(T? value, string? message = null)
    {
        if (value != null)
        {
            throw new AssertionException(message ?? $"Expected null but was: {value}");
        }
    }

    public static void IsNotNull<T>(T? value, string? message = null)
    {
        if (value == null)
        {
            throw new AssertionException(message ?? "Expected non-null value but was null");
        }
    }

    public static void Throws<TException>(Action action, string? message = null)
        where TException : Exception
    {
        try
        {
            action();
            throw new AssertionException(
                message ?? $"Expected exception of type {typeof(TException).Name} but no exception was thrown"
            );
        }
        catch (TException)
        {
            // Expected exception - test passes
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                message ?? $"Expected exception of type {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}"
            );
        }
    }

    public static void Contains<T>(IEnumerable<T> collection, T item, string? message = null)
    {
        if (!collection.Contains(item))
        {
            throw new AssertionException(
                message ?? $"Collection does not contain expected item: {item}"
            );
        }
    }

    public static void Empty<T>(IEnumerable<T> collection, string? message = null)
    {
        if (collection.Any())
        {
            throw new AssertionException(
                message ?? $"Expected empty collection but found {collection.Count()} items"
            );
        }
    }

    public static void NotEmpty<T>(IEnumerable<T> collection, string? message = null)
    {
        if (!collection.Any())
        {
            throw new AssertionException(message ?? "Expected non-empty collection but was empty");
        }
    }

    public static void Count<T>(IEnumerable<T> collection, int expectedCount, string? message = null)
    {
        var actualCount = collection.Count();
        if (actualCount != expectedCount)
        {
            throw new AssertionException(
                message ?? $"Expected collection count: {expectedCount}, Actual: {actualCount}"
            );
        }
    }
}

public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
