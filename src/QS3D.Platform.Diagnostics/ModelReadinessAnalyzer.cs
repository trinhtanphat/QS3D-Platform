using QS3D.Platform.Domain;

namespace QS3D.Platform.Diagnostics;

public static class ModelReadinessAnalyzer
{
    public static ModelHealthReport Analyze(SemanticProject project)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));
        var semantic = SemanticHealthAnalyzer.Analyze(project);
        var ownership = CadOwnershipHealthAnalyzer.Analyze(project);
        return new ModelHealthReport(semantic.Findings.Concat(ownership));
    }
}
