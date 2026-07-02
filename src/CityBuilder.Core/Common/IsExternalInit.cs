using System.ComponentModel;

// Polyfill required so that C# `init`-only setters and positional `record` types
// compile against netstandard2.1 (the type only exists in the BCL from .NET 5+).
// This is a no-op marker type consumed by the compiler.
namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
