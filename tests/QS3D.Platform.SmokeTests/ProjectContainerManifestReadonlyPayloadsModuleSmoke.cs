using System.Runtime.CompilerServices;
using QS3D.Platform.Persistence;

namespace QS3D.Platform.SmokeTests;

internal static class ProjectContainerManifestReadonlyPayloadsModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        const string digest = "0000000000000000000000000000000000000000000000000000000000000000";
        var semantic = new ProjectContainerPayload(ProjectContainerSectionNames.SemanticProject, "application/json", 10, digest);
        var drawing = new ProjectContainerPayload(ProjectContainerSectionNames.DrawingPayload, "application/octet-stream", 20, digest, required: false);
        var manifest = new ProjectContainerManifest(1, Guid.NewGuid(), new[] { semantic, drawing });

        if (manifest.Payloads.Count != 2)
            throw new InvalidOperationException("Expected two manifest payloads for immutability regression.");
        if (!StringComparer.Ordinal.Equals(manifest.Payloads[0].Name, ProjectContainerSectionNames.DrawingPayload) ||
            !StringComparer.Ordinal.Equals(manifest.Payloads[1].Name, ProjectContainerSectionNames.SemanticProject))
            throw new InvalidOperationException("Manifest payload ordering must remain deterministic and ordinal by normalized name.");

        if (manifest.Payloads is not IList<ProjectContainerPayload> mutable)
            throw new InvalidOperationException("Manifest payloads should expose standard indexed read semantics through IList<T>.");

        var original = manifest.Payloads[0];
        var replacement = new ProjectContainerPayload("replacement", "application/octet-stream", 1, digest, required: false);
        Throws<NotSupportedException>(() => mutable[0] = replacement);
        if (!ReferenceEquals(original, manifest.Payloads[0]))
            throw new InvalidOperationException("Manifest payload evidence changed after a rejected mutation.");
        if (!ReferenceEquals(semantic, manifest.GetRequired(ProjectContainerSectionNames.SemanticProject)))
            throw new InvalidOperationException("Manifest required-payload lookup changed after a rejected mutation.");

        Console.WriteLine("PASS project container manifest payload list immutability");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
