using System.Runtime.CompilerServices;
using QS3D.Platform.Application;
using QS3D.Platform.InMemory;

namespace QS3D.Platform.SmokeTests;

internal static class ModuleCompatibilityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var core = new ModuleDescriptor("Core", "Core", new ModuleVersion(1, 2, 0));
        var quantity = new ModuleDescriptor(
            "quantity",
            "Quantity",
            new ModuleVersion(1, 0, 0),
            new[] { new ModuleDependency("CORE", new ModuleVersion(1, 0, 0), new ModuleVersion(2, 0, 0)) });
        var ui = new ModuleDescriptor(
            "ui",
            "UI",
            new ModuleVersion(1, 0, 0),
            new[] { new ModuleDependency("quantity", new ModuleVersion(1, 0, 0)) });

        var plan = new ModuleCatalog(new[] { ui, quantity, core }).PlanLoad();
        Equal("core", plan.Modules[0].Id);
        Equal("quantity", plan.Modules[1].Id);
        Equal("ui", plan.Modules[2].Id);

        Throws<InvalidOperationException>(() => new ModuleCatalog(new[]
        {
            new ModuleDescriptor("consumer", "Consumer", new ModuleVersion(1, 0, 0), new[] { new ModuleDependency("missing", new ModuleVersion(1, 0, 0)) })
        }).PlanLoad());

        Throws<InvalidOperationException>(() => new ModuleCatalog(new[]
        {
            new ModuleDescriptor("core", "Core", new ModuleVersion(2, 0, 0)),
            new ModuleDescriptor("consumer", "Consumer", new ModuleVersion(1, 0, 0), new[] { new ModuleDependency("core", new ModuleVersion(1, 0, 0), new ModuleVersion(2, 0, 0)) })
        }).PlanLoad());

        Throws<InvalidOperationException>(() => new ModuleCatalog(new[]
        {
            new ModuleDescriptor("a", "A", new ModuleVersion(1, 0, 0), new[] { new ModuleDependency("b", new ModuleVersion(1, 0, 0)) }),
            new ModuleDescriptor("b", "B", new ModuleVersion(1, 0, 0), new[] { new ModuleDependency("a", new ModuleVersion(1, 0, 0)) })
        }).PlanLoad());

        Throws<InvalidOperationException>(() => _ = new ModuleCatalog(new[]
        {
            new ModuleDescriptor("CORE", "Core A", new ModuleVersion(1, 0, 0)),
            new ModuleDescriptor("core", "Core B", new ModuleVersion(1, 0, 1))
        }));

        var commands = new CommandRegistry();
        var module = new TestModule();
        module.Register(new CommandModuleRegistrationContext(commands));
        var document = new InMemoryDocumentManager().CreateNew("ModuleTest");
        Require(commands.Execute("modping", new CommandContext(document)).Succeeded, "registered module command must execute");

        Console.WriteLine("PASS module compatibility and load planning");
    }

    private sealed class TestModule : IPlatformModule
    {
        public ModuleDescriptor Descriptor { get; } = new ModuleDescriptor("test.module", "Test Module", new ModuleVersion(1, 0, 0));
        public void Register(IModuleRegistrationContext context) => context.RegisterCommand(new ModulePingCommand());
    }

    private sealed class ModulePingCommand : ICadCommand
    {
        public string Name => "MODPING";
        public CommandFlags Flags => CommandFlags.RequiresDocument | CommandFlags.ReadOnly;
        public CommandResult Execute(CommandContext context) => CommandResult.Success("MODULE PONG");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
