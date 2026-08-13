using QS3D.Platform.Cad.Abstractions;

namespace QS3D.Platform.Application;

[Flags]
public enum CommandFlags
{
    None = 0,
    ReadOnly = 1 << 0,
    RequiresDocument = 1 << 1,
    ModifiesDrawing = 1 << 2
}

public readonly record struct CommandResult(bool Succeeded, string? Message)
{
    public static CommandResult Success(string? message = null) => new(true, message);
    public static CommandResult Failure(string message) => new(false, message);
}

public sealed record CommandContext(ICadDocument Document, CancellationToken CancellationToken = default);

public interface ICadCommand
{
    string Name { get; }
    CommandFlags Flags { get; }
    CommandResult Execute(CommandContext context);
}

public sealed class CommandRegistry
{
    private readonly Dictionary<string, ICadCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Names => _commands.Keys.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase).ToArray();

    public void Register(ICadCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        var name = command.Name.Trim();
        if (!_commands.TryAdd(name, command))
            throw new InvalidOperationException($"Command '{name}' is already registered.");
    }

    public bool TryResolve(string name, out ICadCommand? command)
    {
        command = null;
        return !string.IsNullOrWhiteSpace(name) && _commands.TryGetValue(name.Trim(), out command);
    }

    public CommandResult Execute(string name, CommandContext context)
    {
        if (!TryResolve(name, out var command) || command is null)
            return CommandResult.Failure($"Unknown command '{name}'.");
        context.CancellationToken.ThrowIfCancellationRequested();
        return command.Execute(context);
    }
}
