using System.Globalization;

namespace QS3D.Platform.Persistence;

public static class ProjectContainerSectionNames
{
    public const string SemanticProject = "semantic-project";
    public const string DrawingPayload = "drawing-payload";
}

public sealed class ProjectContainerPayload
{
    public ProjectContainerPayload(string name, string mediaType, long lengthBytes, string sha256Hex, bool required = true)
    {
        Name = NormalizeToken(name, nameof(name));
        if (string.IsNullOrWhiteSpace(mediaType)) throw new ArgumentException("Payload media type must not be blank.", nameof(mediaType));
        if (lengthBytes < 0) throw new ArgumentOutOfRangeException(nameof(lengthBytes));
        MediaType = mediaType.Trim().ToLowerInvariant();
        LengthBytes = lengthBytes;
        Sha256Hex = NormalizeSha256(sha256Hex);
        Required = required;
    }

    public string Name { get; }
    public string MediaType { get; }
    public long LengthBytes { get; }
    public string Sha256Hex { get; }
    public bool Required { get; }

    private static string NormalizeToken(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Payload name must not be blank.", parameterName);
        var normalized = value.Trim().ToLowerInvariant();
        foreach (var c in normalized)
        {
            var valid = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.';
            if (!valid) throw new ArgumentException("Payload name contains an unsupported character.", parameterName);
        }
        return normalized;
    }

    private static string NormalizeSha256(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("SHA-256 digest must not be blank.", nameof(value));
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 64) throw new FormatException("SHA-256 digest must contain exactly 64 hexadecimal characters.");
        for (var index = 0; index < normalized.Length; index++)
        {
            var c = normalized[index];
            var isHex = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F');
            if (!isHex) throw new FormatException($"SHA-256 digest contains non-hexadecimal character at index {index.ToString(CultureInfo.InvariantCulture)}.");
        }
        return normalized;
    }
}

public sealed class ProjectContainerManifest
{
    public ProjectContainerManifest(int formatVersion, Guid projectId, IEnumerable<ProjectContainerPayload> payloads)
    {
        if (formatVersion < 1) throw new ArgumentOutOfRangeException(nameof(formatVersion));
        if (projectId == Guid.Empty) throw new ArgumentException("Project ID must not be empty.", nameof(projectId));
        if (payloads is null) throw new ArgumentNullException(nameof(payloads));

        var byName = new Dictionary<string, ProjectContainerPayload>(StringComparer.Ordinal);
        foreach (var payload in payloads)
        {
            if (payload is null) throw new ArgumentException("Container payloads must not contain null entries.", nameof(payloads));
            if (byName.ContainsKey(payload.Name)) throw new InvalidOperationException($"Duplicate container payload '{payload.Name}'.");
            byName.Add(payload.Name, payload);
        }

        if (!byName.TryGetValue(ProjectContainerSectionNames.SemanticProject, out var semantic) || !semantic.Required)
            throw new InvalidOperationException($"Container must declare required '{ProjectContainerSectionNames.SemanticProject}' payload.");

        FormatVersion = formatVersion;
        ProjectId = projectId;
        Payloads = byName.Values.OrderBy(static payload => payload.Name, StringComparer.Ordinal).ToArray();
    }

    public int FormatVersion { get; }
    public Guid ProjectId { get; }
    public IReadOnlyList<ProjectContainerPayload> Payloads { get; }

    public long TotalDeclaredBytes
    {
        get
        {
            long total = 0;
            foreach (var payload in Payloads)
            {
                checked { total += payload.LengthBytes; }
            }
            return total;
        }
    }

    public ProjectContainerPayload GetRequired(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Payload name must not be blank.", nameof(name));
        var normalized = name.Trim().ToLowerInvariant();
        return Payloads.FirstOrDefault(payload => StringComparer.Ordinal.Equals(payload.Name, normalized))
            ?? throw new KeyNotFoundException($"Container payload '{normalized}' is not declared.");
    }
}

public static class ProjectContainerManifestValidator
{
    public static void ValidatePayload(ProjectContainerManifest manifest, string payloadName, long actualLengthBytes, string actualSha256Hex)
    {
        if (manifest is null) throw new ArgumentNullException(nameof(manifest));
        if (actualLengthBytes < 0) throw new ArgumentOutOfRangeException(nameof(actualLengthBytes));
        var expected = manifest.GetRequired(payloadName);
        if (actualLengthBytes != expected.LengthBytes)
            throw new InvalidDataException($"Payload '{expected.Name}' length mismatch: expected {expected.LengthBytes}, got {actualLengthBytes}.");
        var actual = new ProjectContainerPayload(expected.Name, expected.MediaType, actualLengthBytes, actualSha256Hex, expected.Required);
        if (!StringComparer.Ordinal.Equals(actual.Sha256Hex, expected.Sha256Hex))
            throw new InvalidDataException($"Payload '{expected.Name}' SHA-256 mismatch.");
    }
}
