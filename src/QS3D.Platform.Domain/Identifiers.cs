namespace QS3D.Platform.Domain;

public readonly record struct ProjectId(Guid Value)
{
    public static ProjectId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct FloorId(Guid Value)
{
    public static FloorId New() => new(Guid.NewGuid());
}

public readonly record struct ZoneId(Guid Value)
{
    public static ZoneId New() => new(Guid.NewGuid());
}

public readonly record struct FamilyId(Guid Value)
{
    public static FamilyId New() => new(Guid.NewGuid());
}

public readonly record struct ElementId(Guid Value)
{
    public static ElementId New() => new(Guid.NewGuid());
}

public readonly record struct DrawingId(Guid Value)
{
    public static DrawingId New() => new(Guid.NewGuid());
}

public readonly struct CadHandle : IEquatable<CadHandle>, IComparable<CadHandle>
{
    public CadHandle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CAD handle must not be blank.", nameof(value));
        var token = value.Trim();
        foreach (var c in token)
        {
            if (!Uri.IsHexDigit(c))
                throw new FormatException($"CAD handle '{value}' is not hexadecimal.");
        }

        token = token.TrimStart('0');
        Value = (token.Length == 0 ? "0" : token).ToUpperInvariant();
    }

    public string Value { get; }
    public bool Equals(CadHandle other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is CadHandle other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
    public int CompareTo(CadHandle other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(CadHandle left, CadHandle right) => left.Equals(right);
    public static bool operator !=(CadHandle left, CadHandle right) => !left.Equals(right);
}

public readonly record struct CadReference(DrawingId DrawingId, CadHandle Handle);
