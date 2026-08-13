using QS3D.Platform.Cad.Abstractions;

namespace QS3D.Platform.InMemory;

public sealed class InMemoryXrefService : ICadXrefService
{
    private readonly Dictionary<string, CadXrefSnapshot> _xrefs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<string, bool> _pathExists;

    public InMemoryXrefService(Func<string, bool>? pathExists = null)
        => _pathExists = pathExists ?? File.Exists;

    public IReadOnlyList<CadXrefSnapshot> GetXrefs()
        => _xrefs.Values.OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Name, StringComparer.Ordinal).ToArray();

    public CadXrefSnapshot Attach(string path, string name, CadXrefKind kind)
    {
        var normalizedName = Normalize(name, "Xref name");
        var normalizedPath = Normalize(path, "Xref path");
        if (_xrefs.ContainsKey(normalizedName)) throw new InvalidOperationException($"Xref '{normalizedName}' already exists.");
        var snapshot = new CadXrefSnapshot(normalizedName, normalizedPath, kind,
            _pathExists(normalizedPath) ? CadXrefStatus.Loaded : CadXrefStatus.Missing);
        _xrefs.Add(normalizedName, snapshot);
        return snapshot;
    }

    public void Reload(string name)
    {
        var current = Require(name);
        _xrefs[current.Name] = current with { Status = _pathExists(current.Path) ? CadXrefStatus.Loaded : CadXrefStatus.Missing };
    }

    public void Unload(string name)
    {
        var current = Require(name);
        _xrefs[current.Name] = current with { Status = CadXrefStatus.Unloaded };
    }

    public void Detach(string name)
    {
        var current = Require(name);
        _xrefs.Remove(current.Name);
    }

    private CadXrefSnapshot Require(string name)
    {
        var normalized = Normalize(name, "Xref name");
        return _xrefs.TryGetValue(normalized, out var current)
            ? current
            : throw new KeyNotFoundException($"Xref '{normalized}' does not exist.");
    }

    private static string Normalize(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{label} must not be blank.", nameof(value));
        return value.Trim();
    }
}
