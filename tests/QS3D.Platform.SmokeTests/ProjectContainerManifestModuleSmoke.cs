using System.Runtime.CompilerServices;
using QS3D.Platform.Persistence;

namespace QS3D.Platform.SmokeTests;

internal static class ProjectContainerManifestModuleSmoke
{
    private const string SemanticHash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private const string DrawingHash = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    [ModuleInitializer]
    internal static void Run()
    {
        var projectId = Guid.NewGuid();
        var manifest = new ProjectContainerManifest(1, projectId, new[]
        {
            new ProjectContainerPayload(ProjectContainerSectionNames.SemanticProject, "application/vnd.qs3d.semantic+json", 128, SemanticHash),
            new ProjectContainerPayload(ProjectContainerSectionNames.DrawingPayload, "application/octet-stream", 1024, DrawingHash, required: false)
        });

        Equal(projectId, manifest.ProjectId);
        Equal(1152L, manifest.TotalDeclaredBytes);
        Equal(ProjectContainerSectionNames.SemanticProject, manifest.GetRequired("SEMANTIC-PROJECT").Name);
        ProjectContainerManifestValidator.ValidatePayload(manifest, ProjectContainerSectionNames.SemanticProject, 128, SemanticHash.ToLowerInvariant());

        Throws<InvalidOperationException>(() => _ = new ProjectContainerManifest(1, projectId, new[]
        {
            new ProjectContainerPayload(ProjectContainerSectionNames.DrawingPayload, "application/octet-stream", 1, DrawingHash)
        }));
        Throws<InvalidOperationException>(() => _ = new ProjectContainerManifest(1, projectId, new[]
        {
            new ProjectContainerPayload(ProjectContainerSectionNames.SemanticProject, "application/json", 1, SemanticHash),
            new ProjectContainerPayload("SEMANTIC-PROJECT", "application/json", 1, SemanticHash)
        }));
        Throws<InvalidDataException>(() => ProjectContainerManifestValidator.ValidatePayload(manifest, ProjectContainerSectionNames.SemanticProject, 127, SemanticHash));
        Throws<InvalidDataException>(() => ProjectContainerManifestValidator.ValidatePayload(manifest, ProjectContainerSectionNames.SemanticProject, 128, DrawingHash));
        Throws<FormatException>(() => _ = new ProjectContainerPayload("bad", "application/octet-stream", 0, "not-a-digest"));

        Console.WriteLine("PASS project container manifest contracts");
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
