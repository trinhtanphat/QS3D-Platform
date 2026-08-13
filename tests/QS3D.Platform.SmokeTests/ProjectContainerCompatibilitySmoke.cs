using System.Runtime.CompilerServices;
using QS3D.Platform.Persistence;

namespace QS3D.Platform.SmokeTests;

internal static class ProjectContainerCompatibilitySmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var payload = new byte[] { 1, 2, 3 };
        var manifest = new ProjectContainerManifest(1, Guid.NewGuid(), new[]
        {
            new ProjectContainerPayload(ProjectContainerSectionNames.SemanticProject, "application/json", payload.Length, ProjectContainerManifest.Hash(payload))
        });
        ProjectContainerManifestValidator.ValidatePayloadSet(manifest, new Dictionary<string, byte[]>
        {
            [ProjectContainerSectionNames.SemanticProject] = payload
        });
        ExpectInvalid(() => ProjectContainerManifestValidator.ValidatePayloadSet(manifest, new Dictionary<string, byte[]>()));
        ExpectInvalid(() => ProjectContainerManifestValidator.ValidatePayloadSet(manifest, new Dictionary<string, byte[]>
        {
            [ProjectContainerSectionNames.SemanticProject] = payload,
            ["unexpected"] = new byte[] { 9 }
        }));
    }

    private static void ExpectInvalid(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Expected invalid container payload set.");
    }
}
