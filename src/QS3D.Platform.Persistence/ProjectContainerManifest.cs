using System.Globalization;
using System.Security.Cryptography;
using System.Text;

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

    internal static string NormalizeToken(string value, string parameterName)
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

        var materializedPayloads = SnapshotGuard.Copy(payloads, nameof(payloads));
        var byName = new Dictionary<string, ProjectContainerPayload>(StringComparer.Ordinal);
        foreach (var payload in materializedPayloads)
        {
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
        var normalized = ProjectContainerPayload.NormalizeToken(name, nameof(name));
        return Payloads.FirstOrDefault(payload => StringComparer.Ordinal.Equals(payload.Name, normalized))
            ?? throw new KeyNotFoundException($"Container payload '{normalized}' is not declared.");
    }

    public static string Hash(byte[] payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        byte[] digest;
        using (var sha256 = SHA256.Create()) digest = sha256.ComputeHash(payload);
        var builder = new StringBuilder(digest.Length * 2);
        foreach (var value in digest) builder.Append(value.ToString("X2", CultureInfo.InvariantCulture));
        return builder.ToString();
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

    public static void ValidatePayloadSet(ProjectContainerManifest manifest, IReadOnlyDictionary<string, byte[]> payloads)
    {
        if (manifest is null) throw new ArgumentNullException(nameof(manifest));
        if (payloads is null) throw new ArgumentNullException(nameof(payloads));

        var advertisedCount = payloads.Count;
        ValidatePayloadSetCount(advertisedCount);

        var actualByName = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var pair in payloads)
        {
            if (actualByName.Count >= SnapshotGuard.MaxCollectionEntries)
                throw new ArgumentException($"Payload set exceeds the {SnapshotGuard.MaxCollectionEntries} entry limit.", nameof(payloads));

            var normalized = ProjectContainerPayload.NormalizeToken(pair.Key, nameof(payloads));
            if (pair.Value is null) throw new InvalidDataException($"Payload '{normalized}' content is null.");
            if (actualByName.ContainsKey(normalized)) throw new InvalidDataException($"Payload set contains duplicate normalized name '{normalized}'.");
            actualByName.Add(normalized, pair.Value);
        }

        if (actualByName.Count != advertisedCount)
            throw new ArgumentException("Payload set Count does not match enumeration.", nameof(payloads));

        var finalCount = payloads.Count;
        ValidatePayloadSetCount(finalCount);
        if (finalCount != advertisedCount || finalCount != actualByName.Count)
            throw new ArgumentException("Payload set Count changed during materialization.", nameof(payloads));

        var declared = new HashSet<string>(manifest.Payloads.Select(static payload => payload.Name), StringComparer.Ordinal);
        foreach (var actualName in actualByName.Keys)
        {
            if (!declared.Contains(actualName)) throw new InvalidDataException($"Payload set contains unexpected payload '{actualName}'.");
        }

        foreach (var expected in manifest.Payloads)
        {
            if (!actualByName.TryGetValue(expected.Name, out var bytes))
            {
                if (expected.Required) throw new InvalidDataException($"Required payload '{expected.Name}' is missing.");
                continue;
            }
            ValidatePayload(manifest, expected.Name, bytes.LongLength, ProjectContainerManifest.Hash(bytes));
        }
    }

    private static void ValidatePayloadSetCount(int count)
    {
        if (count < 0)
            throw new ArgumentException("Payload set Count must not be negative.", "payloads");
        if (count > SnapshotGuard.MaxCollectionEntries)
            throw new ArgumentException($"Payload set exceeds the {SnapshotGuard.MaxCollectionEntries} entry limit.", "payloads");
    }
}
