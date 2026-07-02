namespace CityBuilder.Commands;

/// <summary>Outcome of attempting a command. Value type — no allocation on the hot path.</summary>
public readonly struct CommandResult
{
    public readonly bool Success;
    public readonly string? Message;

    private CommandResult(bool success, string? message)
    {
        Success = success;
        Message = message;
    }

    public static readonly CommandResult Ok = new(true, null);

    public static CommandResult Succeeded(string message) => new(true, message);

    public static CommandResult Fail(string message) => new(false, message);
}
