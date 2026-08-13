using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.Families;

public enum FamilyParameterType
{
    Text = 0,
    Boolean,
    Integer,
    Scalar,
    Quantity
}

public sealed class FamilyParameterValue : IEquatable<FamilyParameterValue>
{
    private FamilyParameterValue(FamilyParameterType type, string? text, bool boolean, long integer, double scalar, QuantityValue quantity)
    {
        Type = type;
        Text = text;
        Boolean = boolean;
        Integer = integer;
        Scalar = scalar;
        Quantity = quantity;
    }

    public FamilyParameterType Type { get; }
    public string? Text { get; }
    public bool Boolean { get; }
    public long Integer { get; }
    public double Scalar { get; }
    public QuantityValue Quantity { get; }

    public static FamilyParameterValue FromText(string value)
        => new(FamilyParameterType.Text, value ?? throw new ArgumentNullException(nameof(value)), false, 0L, 0d, default);
    public static FamilyParameterValue FromBoolean(bool value)
        => new(FamilyParameterType.Boolean, null, value, 0L, 0d, default);
    public static FamilyParameterValue FromInteger(long value)
        => new(FamilyParameterType.Integer, null, false, value, 0d, default);
    public static FamilyParameterValue FromScalar(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
        return new FamilyParameterValue(FamilyParameterType.Scalar, null, false, 0L, value, default);
    }
    public static FamilyParameterValue FromQuantity(QuantityValue value)
        => new(FamilyParameterType.Quantity, null, false, 0L, 0d, value);

    public bool Equals(FamilyParameterValue? other)
    {
        if (other is null || Type != other.Type) return false;
        return Type switch
        {
            FamilyParameterType.Text => StringComparer.Ordinal.Equals(Text, other.Text),
            FamilyParameterType.Boolean => Boolean == other.Boolean,
            FamilyParameterType.Integer => Integer == other.Integer,
            FamilyParameterType.Scalar => Scalar.Equals(other.Scalar),
            FamilyParameterType.Quantity => Quantity.Equals(other.Quantity),
            _ => false
        };
    }

    public override bool Equals(object? obj) => Equals(obj as FamilyParameterValue);
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int)Type * 397;
            return Type switch
            {
                FamilyParameterType.Text => hash ^ StringComparer.Ordinal.GetHashCode(Text ?? string.Empty),
                FamilyParameterType.Boolean => hash ^ Boolean.GetHashCode(),
                FamilyParameterType.Integer => hash ^ Integer.GetHashCode(),
                FamilyParameterType.Scalar => hash ^ Scalar.GetHashCode(),
                FamilyParameterType.Quantity => hash ^ Quantity.GetHashCode(),
                _ => hash
            };
        }
    }
}

public sealed class FamilyParameterDefinition
{
    public FamilyParameterDefinition(
        string name,
        FamilyParameterType type,
        bool required = false,
        FamilyParameterValue? defaultValue = null,
        QuantityDimension? quantityDimension = null,
        double? minimum = null,
        double? maximum = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parameter name must not be blank.", nameof(name));
        Name = NormalizeName(name);
        Type = type;
        Required = required;
        QuantityDimension = quantityDimension;
        Minimum = ValidateBound(minimum, nameof(minimum));
        Maximum = ValidateBound(maximum, nameof(maximum));
        if (Minimum.HasValue && Maximum.HasValue && Minimum.Value > Maximum.Value)
            throw new ArgumentException("Minimum must not exceed maximum.");
        if (type == FamilyParameterType.Quantity && !quantityDimension.HasValue)
            throw new ArgumentException("Quantity parameter must declare a quantity dimension.", nameof(quantityDimension));
        if (type != FamilyParameterType.Quantity && quantityDimension.HasValue)
            throw new ArgumentException("Only quantity parameters may declare a quantity dimension.", nameof(quantityDimension));
        if ((type == FamilyParameterType.Text || type == FamilyParameterType.Boolean) && (Minimum.HasValue || Maximum.HasValue))
            throw new ArgumentException("Text and Boolean parameters do not support numeric bounds.");
        if (defaultValue is not null) ValidateValue(defaultValue);
        DefaultValue = defaultValue;
    }

    public string Name { get; }
    public FamilyParameterType Type { get; }
    public bool Required { get; }
    public FamilyParameterValue? DefaultValue { get; }
    public QuantityDimension? QuantityDimension { get; }
    public double? Minimum { get; }
    public double? Maximum { get; }

    public void ValidateValue(FamilyParameterValue value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Type != Type) throw new InvalidOperationException($"Parameter '{Name}' expects {Type}, got {value.Type}.");
        if (Type == FamilyParameterType.Quantity && value.Quantity.Dimension != QuantityDimension)
            throw new InvalidOperationException($"Parameter '{Name}' expects quantity dimension {QuantityDimension}, got {value.Quantity.Dimension}.");
        var numeric = NumericValue(value);
        if (numeric.HasValue && Minimum.HasValue && numeric.Value < Minimum.Value)
            throw new InvalidOperationException($"Parameter '{Name}' is below minimum {Minimum.Value:R}.");
        if (numeric.HasValue && Maximum.HasValue && numeric.Value > Maximum.Value)
            throw new InvalidOperationException($"Parameter '{Name}' exceeds maximum {Maximum.Value:R}.");
    }

    private double? NumericValue(FamilyParameterValue value)
        => Type switch
        {
            FamilyParameterType.Integer => value.Integer,
            FamilyParameterType.Scalar => value.Scalar,
            FamilyParameterType.Quantity => value.Quantity.Value,
            _ => null
        };

    private static double? ValidateBound(double? value, string name)
    {
        if (!value.HasValue) return null;
        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value)) throw new ArgumentOutOfRangeException(name);
        return value;
    }

    private static string NormalizeName(string value)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0) throw new ArgumentException("Parameter name must not be blank.", nameof(value));
        return normalized;
    }
}

public sealed class FamilySchemaDefinition
{
    public FamilySchemaDefinition(string schemaId, int version, SemanticElementKind kind, string name, IEnumerable<FamilyParameterDefinition> parameters)
    {
        if (string.IsNullOrWhiteSpace(schemaId)) throw new ArgumentException("Family schema ID must not be blank.", nameof(schemaId));
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        if (kind == SemanticElementKind.Unknown) throw new ArgumentOutOfRangeException(nameof(kind));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Family schema name must not be blank.", nameof(name));
        SchemaId = NormalizeId(schemaId);
        Version = version;
        Kind = kind;
        Name = name.Trim();
        Parameters = Copy(parameters, nameof(parameters));
        var duplicate = Parameters.GroupBy(static parameter => parameter.Name, StringComparer.Ordinal).FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException($"Duplicate family parameter '{duplicate.Key}'.");
    }

    public string SchemaId { get; }
    public int Version { get; }
    public SemanticElementKind Kind { get; }
    public string Name { get; }
    public IReadOnlyList<FamilyParameterDefinition> Parameters { get; }

    public FamilyParameterDefinition GetParameter(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parameter name must not be blank.", nameof(name));
        return Parameters.FirstOrDefault(parameter => StringComparer.Ordinal.Equals(parameter.Name, name.Trim()))
            ?? throw new KeyNotFoundException($"Family parameter '{name.Trim()}' is not defined by schema '{SchemaId}' v{Version}.");
    }

    private static string NormalizeId(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        foreach (var character in normalized)
        {
            var valid = (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '.' || character == '-' || character == '_';
            if (!valid) throw new ArgumentException("Family schema ID contains an unsupported character.", nameof(value));
        }
        return normalized;
    }

    private static T[] Copy<T>(IEnumerable<T> values, string parameterName) where T : class
    {
        if (values is null) throw new ArgumentNullException(parameterName);
        var copied = values.ToArray();
        if (copied.Any(static item => item is null)) throw new ArgumentException("Family schema collection must not contain null entries.", parameterName);
        return copied;
    }
}

public sealed class FamilyParameterSet
{
    private readonly Dictionary<string, FamilyParameterValue> _values;

    public FamilyParameterSet(string schemaId, int schemaVersion, IEnumerable<KeyValuePair<string, FamilyParameterValue>>? values = null)
    {
        if (string.IsNullOrWhiteSpace(schemaId)) throw new ArgumentException("Schema ID must not be blank.", nameof(schemaId));
        if (schemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        SchemaId = schemaId.Trim().ToLowerInvariant();
        SchemaVersion = schemaVersion;
        _values = new Dictionary<string, FamilyParameterValue>(StringComparer.Ordinal);
        if (values is null) return;
        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key)) throw new ArgumentException("Parameter key must not be blank.", nameof(values));
            if (pair.Value is null) throw new ArgumentException("Parameter value must not be null.", nameof(values));
            _values.Add(pair.Key.Trim(), pair.Value);
        }
    }

    public string SchemaId { get; }
    public int SchemaVersion { get; }
    public IReadOnlyDictionary<string, FamilyParameterValue> Values => _values;

    public FamilyParameterSet With(string name, FamilyParameterValue value)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parameter name must not be blank.", nameof(name));
        if (value is null) throw new ArgumentNullException(nameof(value));
        var copy = new Dictionary<string, FamilyParameterValue>(_values, StringComparer.Ordinal) { [name.Trim()] = value };
        return new FamilyParameterSet(SchemaId, SchemaVersion, copy);
    }

    public FamilyParameterSet Without(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parameter name must not be blank.", nameof(name));
        var copy = new Dictionary<string, FamilyParameterValue>(_values, StringComparer.Ordinal);
        copy.Remove(name.Trim());
        return new FamilyParameterSet(SchemaId, SchemaVersion, copy);
    }

    public FamilyParameterSet AtVersion(int version)
        => new(SchemaId, version, _values);
}

public static class FamilySchemaValidator
{
    public static void Validate(FamilySchemaDefinition schema, FamilyParameterSet values, bool rejectUnknown = true)
    {
        if (schema is null) throw new ArgumentNullException(nameof(schema));
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (!StringComparer.Ordinal.Equals(schema.SchemaId, values.SchemaId)) throw new InvalidOperationException("Family parameter set schema ID does not match definition.");
        if (schema.Version != values.SchemaVersion) throw new InvalidOperationException("Family parameter set version does not match definition.");
        var definitions = schema.Parameters.ToDictionary(static parameter => parameter.Name, StringComparer.Ordinal);
        if (rejectUnknown)
        {
            foreach (var key in values.Values.Keys)
                if (!definitions.ContainsKey(key)) throw new InvalidOperationException($"Unknown family parameter '{key}'.");
        }
        foreach (var definition in schema.Parameters)
        {
            if (!values.Values.TryGetValue(definition.Name, out var value))
            {
                if (definition.Required && definition.DefaultValue is null) throw new InvalidOperationException($"Required family parameter '{definition.Name}' is missing.");
                continue;
            }
            definition.ValidateValue(value);
        }
    }

    public static FamilyParameterSet ApplyDefaults(FamilySchemaDefinition schema, FamilyParameterSet values)
    {
        if (schema is null) throw new ArgumentNullException(nameof(schema));
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (!StringComparer.Ordinal.Equals(schema.SchemaId, values.SchemaId) || schema.Version != values.SchemaVersion)
            throw new InvalidOperationException("Family parameter set identity does not match schema.");
        var result = values;
        foreach (var definition in schema.Parameters)
            if (!result.Values.ContainsKey(definition.Name) && definition.DefaultValue is not null)
                result = result.With(definition.Name, definition.DefaultValue);
        Validate(schema, result);
        return result;
    }
}
