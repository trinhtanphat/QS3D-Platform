namespace QS3D.Platform.Application;

public readonly struct ModuleVersion : IEquatable<ModuleVersion>, IComparable<ModuleVersion>
{
    public ModuleVersion(int major, int minor, int patch)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major));
        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor));
        if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch));
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public int CompareTo(ModuleVersion other)
    {
        var major = Major.CompareTo(other.Major);
        if (major != 0) return major;
        var minor = Minor.CompareTo(other.Minor);
        return minor != 0 ? minor : Patch.CompareTo(other.Patch);
    }

    public bool Equals(ModuleVersion other) => Major == other.Major && Minor == other.Minor && Patch == other.Patch;
    public override bool Equals(object? obj) => obj is ModuleVersion other && Equals(other);
    public override int GetHashCode()
    {
        unchecked { return ((Major * 397) ^ Minor) * 397 ^ Patch; }
    }
    public override string ToString() => $"{Major}.{Minor}.{Patch}";
    public static bool operator <(ModuleVersion left, ModuleVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(ModuleVersion left, ModuleVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(ModuleVersion left, ModuleVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(ModuleVersion left, ModuleVersion right) => left.CompareTo(right) >= 0;
}

public sealed class ModuleDependency
{
    public ModuleDependency(string moduleId, ModuleVersion minimumVersion, ModuleVersion? maximumExclusiveVersion = null)
    {
        ModuleId = ModuleIdentity.Normalize(moduleId);
        if (maximumExclusiveVersion.HasValue && maximumExclusiveVersion.Value <= minimumVersion)
            throw new ArgumentException("Maximum exclusive module version must be greater than the minimum version.", nameof(maximumExclusiveVersion));
        MinimumVersion = minimumVersion;
        MaximumExclusiveVersion = maximumExclusiveVersion;
    }

    public string ModuleId { get; }
    public ModuleVersion MinimumVersion { get; }
    public ModuleVersion? MaximumExclusiveVersion { get; }

    public bool Accepts(ModuleVersion version)
        => version >= MinimumVersion && (!MaximumExclusiveVersion.HasValue || version < MaximumExclusiveVersion.Value);
}

public sealed class ModuleDescriptor
{
    public ModuleDescriptor(string id, string name, ModuleVersion version, IEnumerable<ModuleDependency>? dependencies = null)
    {
        Id = ModuleIdentity.Normalize(id);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Module name must not be blank.", nameof(name));
        Name = name.Trim();
        Version = version;
        var copied = dependencies is null ? Array.Empty<ModuleDependency>() : dependencies.ToArray();
        if (copied.Any(static dependency => dependency is null)) throw new ArgumentException("Module dependencies must not contain null entries.", nameof(dependencies));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dependency in copied)
        {
            if (!seen.Add(dependency.ModuleId)) throw new InvalidOperationException($"Module '{Id}' declares dependency '{dependency.ModuleId}' more than once.");
        }
        Dependencies = copied.OrderBy(static dependency => dependency.ModuleId, StringComparer.Ordinal).ToArray();
    }

    public string Id { get; }
    public string Name { get; }
    public ModuleVersion Version { get; }
    public IReadOnlyList<ModuleDependency> Dependencies { get; }
}

public sealed class ModuleLoadPlan
{
    public ModuleLoadPlan(IEnumerable<ModuleDescriptor> modules)
    {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        Modules = modules.ToArray();
    }
    public IReadOnlyList<ModuleDescriptor> Modules { get; }
}

public sealed class ModuleCatalog
{
    private readonly Dictionary<string, ModuleDescriptor> _modules = new(StringComparer.Ordinal);

    public ModuleCatalog(IEnumerable<ModuleDescriptor> modules)
    {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        foreach (var module in modules)
        {
            if (module is null) throw new ArgumentException("Module catalog must not contain null entries.", nameof(modules));
            if (_modules.ContainsKey(module.Id)) throw new InvalidOperationException($"Duplicate module ID '{module.Id}'.");
            _modules.Add(module.Id, module);
        }
    }

    public IReadOnlyCollection<ModuleDescriptor> Modules => _modules.Values.OrderBy(static module => module.Id, StringComparer.Ordinal).ToArray();

    public ModuleLoadPlan PlanLoad()
    {
        ValidateDependencies();
        var ordered = new List<ModuleDescriptor>();
        var states = new Dictionary<string, VisitState>(StringComparer.Ordinal);
        foreach (var module in _modules.Values.OrderBy(static module => module.Id, StringComparer.Ordinal))
            Visit(module, states, ordered);
        return new ModuleLoadPlan(ordered);
    }

    private void ValidateDependencies()
    {
        foreach (var module in _modules.Values)
        {
            foreach (var dependency in module.Dependencies)
            {
                if (!_modules.TryGetValue(dependency.ModuleId, out var target))
                    throw new InvalidOperationException($"Module '{module.Id}' requires missing module '{dependency.ModuleId}'.");
                if (!dependency.Accepts(target.Version))
                    throw new InvalidOperationException($"Module '{module.Id}' requires '{dependency.ModuleId}' >= {dependency.MinimumVersion}"
                        + (dependency.MaximumExclusiveVersion.HasValue ? $" and < {dependency.MaximumExclusiveVersion.Value}" : string.Empty)
                        + $", but catalog contains {target.Version}.");
            }
        }
    }

    private void Visit(ModuleDescriptor module, Dictionary<string, VisitState> states, List<ModuleDescriptor> ordered)
    {
        if (states.TryGetValue(module.Id, out var state))
        {
            if (state == VisitState.Visited) return;
            if (state == VisitState.Visiting) throw new InvalidOperationException($"Module dependency cycle detected at '{module.Id}'.");
        }
        states[module.Id] = VisitState.Visiting;
        foreach (var dependency in module.Dependencies)
            Visit(_modules[dependency.ModuleId], states, ordered);
        states[module.Id] = VisitState.Visited;
        ordered.Add(module);
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }
}

public interface IModuleRegistrationContext
{
    void RegisterCommand(ICadCommand command);
}

public interface IPlatformModule
{
    ModuleDescriptor Descriptor { get; }
    void Register(IModuleRegistrationContext context);
}

public sealed class CommandModuleRegistrationContext : IModuleRegistrationContext
{
    private readonly CommandRegistry _commands;
    public CommandModuleRegistrationContext(CommandRegistry commands)
        => _commands = commands ?? throw new ArgumentNullException(nameof(commands));
    public void RegisterCommand(ICadCommand command) => _commands.Register(command);
}

internal static class ModuleIdentity
{
    public static string Normalize(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Module ID must not be blank.", nameof(id));
        var normalized = id.Trim().ToLowerInvariant();
        foreach (var character in normalized)
        {
            var valid = (character >= 'a' && character <= 'z')
                || (character >= '0' && character <= '9')
                || character == '.' || character == '-' || character == '_';
            if (!valid) throw new ArgumentException("Module ID may contain only ASCII letters, digits, '.', '-' and '_'.", nameof(id));
        }
        return normalized;
    }
}
