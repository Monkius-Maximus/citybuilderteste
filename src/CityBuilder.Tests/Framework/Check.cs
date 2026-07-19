namespace CityBuilder.Tests.Framework;

/// <summary>Thrown by a failing assertion; carried up to the runner and reported.</summary>
public sealed class TestFailure : Exception
{
    public TestFailure(string message) : base(message)
    {
    }
}

/// <summary>Minimal assertion helpers — throw <see cref="TestFailure"/> on failure.</summary>
public static class Check
{
    public static void True(bool condition, string message = "expected true")
    {
        if (!condition)
        {
            throw new TestFailure(message);
        }
    }

    public static void False(bool condition, string message = "expected false")
    {
        if (condition)
        {
            throw new TestFailure(message);
        }
    }

    public static void Equal<T>(T expected, T actual, string context = "")
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new TestFailure($"{context}: expected <{expected}>, got <{actual}>");
        }
    }

    public static void NotEqual<T>(T a, T b, string context = "")
    {
        if (EqualityComparer<T>.Default.Equals(a, b))
        {
            throw new TestFailure($"{context}: values are equal <{a}>");
        }
    }

    public static void Near(double expected, double actual, double tolerance, string context = "")
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new TestFailure($"{context}: expected ~{expected}, got {actual} (tol {tolerance})");
        }
    }

    public static void Throws<TException>(Action action, string context = "")
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new TestFailure($"{context}: expected {typeof(TException).Name}, got {ex.GetType().Name}");
        }

        throw new TestFailure($"{context}: expected {typeof(TException).Name}, nothing thrown");
    }
}
