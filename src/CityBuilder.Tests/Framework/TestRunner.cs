using System.Reflection;

namespace CityBuilder.Tests.Framework;

/// <summary>
/// Reflection-based test runner: finds every <see cref="TestCaseAttribute"/> method in this
/// assembly, runs it, and reports pass/fail. Returns the failure count so the process exit code
/// is a usable CI gate. No external test framework.
/// </summary>
public static class TestRunner
{
    public static int RunAll()
    {
        MethodInfo[] tests = Assembly.GetExecutingAssembly()
            .GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.GetCustomAttribute<TestCaseAttribute>() is not null)
            .OrderBy(m => m.DeclaringType!.Name, StringComparer.Ordinal)
            .ThenBy(m => m.Name, StringComparer.Ordinal)
            .ToArray();

        int passed = 0;
        var failures = new List<string>();
        string? currentClass = null;

        foreach (MethodInfo test in tests)
        {
            string className = test.DeclaringType!.Name;
            if (className != currentClass)
            {
                currentClass = className;
                Console.WriteLine($"\n{className}");
            }

            string name = test.Name;
            try
            {
                test.Invoke(null, null);
                Console.WriteLine($"  [ok]   {name}");
                passed++;
            }
            catch (Exception ex)
            {
                Exception real = ex is TargetInvocationException tie && tie.InnerException is not null ? tie.InnerException : ex;
                Console.WriteLine($"  [FAIL] {name}: {real.Message}");
                failures.Add($"{className}.{name}: {real.Message}");
            }
        }

        Console.WriteLine($"\n=====================================");
        Console.WriteLine($"{passed} passed, {failures.Count} failed, {tests.Length} total.");
        foreach (string f in failures)
        {
            Console.WriteLine($"  FAILED: {f}");
        }

        return failures.Count;
    }
}
