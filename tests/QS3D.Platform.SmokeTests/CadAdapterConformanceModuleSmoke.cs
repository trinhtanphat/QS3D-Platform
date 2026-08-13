using System.Runtime.CompilerServices;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.InMemory;

namespace QS3D.Platform.SmokeTests;

internal static class CadAdapterConformanceModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        using var fixture = new InMemoryFixture();
        var report = CadAdapterConformance.Run(fixture);
        if (!report.Passed)
        {
            var detail = string.Join(Environment.NewLine, report.Findings.Select(static finding => $"{finding.Code}: {finding.Message}"));
            throw new InvalidOperationException("In-memory CAD adapter failed shared conformance:" + Environment.NewLine + detail);
        }
        Equal(0, report.ErrorCount);
        Equal(6, report.Findings.Count);
        Console.WriteLine("PASS shared CAD adapter conformance harness");
    }

    private sealed class InMemoryFixture : ICadConformanceFixture
    {
        private readonly InMemoryDocumentManager _documents = new();
        public ICadDocument CreateIsolatedDocument(string name) => _documents.CreateNew(name);
        public void Dispose()
        {
            foreach (var document in _documents.Documents.ToArray()) _documents.Close(document.Id);
        }
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }
}
