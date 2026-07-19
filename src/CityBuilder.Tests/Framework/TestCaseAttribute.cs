namespace CityBuilder.Tests.Framework;

/// <summary>Marks a public static, parameterless method as a test the runner should execute.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestCaseAttribute : Attribute
{
}
