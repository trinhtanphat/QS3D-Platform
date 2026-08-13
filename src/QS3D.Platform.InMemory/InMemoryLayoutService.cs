using QS3D.Platform.Cad.Abstractions;

namespace QS3D.Platform.InMemory;

public sealed class InMemoryLayoutService : ICadLayoutService
{
    private readonly Dictionary<string, CadLayoutSnapshot> _layouts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Model"] = new CadLayoutSnapshot("Model", true, 0d, 0d)
    };

    public IReadOnlyList<CadLayoutSnapshot> GetLayouts()
        => _layouts.Values.OrderByDescending(static layout => layout.IsModel)
            .ThenBy(static layout => layout.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static layout => layout.Name, StringComparer.Ordinal).ToArray();

    public string CurrentLayoutName { get; private set; } = "Model";

    public void SetCurrent(string name)
    {
        var layout = Require(name);
        CurrentLayoutName = layout.Name;
    }

    public CadLayoutSnapshot Create(string name)
    {
        var normalized = Normalize(name);
        if (_layouts.ContainsKey(normalized)) throw new InvalidOperationException($"Layout '{normalized}' already exists.");
        var layout = new CadLayoutSnapshot(normalized, false, 210d, 297d);
        _layouts.Add(normalized, layout);
        return layout;
    }

    public void Delete(string name)
    {
        var layout = Require(name);
        if (layout.IsModel) throw new InvalidOperationException("Model layout cannot be deleted.");
        if (StringComparer.OrdinalIgnoreCase.Equals(CurrentLayoutName, layout.Name))
            throw new InvalidOperationException("Current layout cannot be deleted.");
        _layouts.Remove(layout.Name);
    }

    private CadLayoutSnapshot Require(string name)
    {
        var normalized = Normalize(name);
        return _layouts.TryGetValue(normalized, out var layout)
            ? layout
            : throw new KeyNotFoundException($"Layout '{normalized}' does not exist.");
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Layout name must not be blank.", nameof(value));
        return value.Trim();
    }
}
