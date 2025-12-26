using DiagnosticsToolkit.Models;

namespace DiagnosticsToolkit.Analyzer.Rules;

/// <summary>
/// Detects excessive garbage collection cycles that can harm performance.
/// </summary>
public class GcThresholdRule : DiagnosticRule
{
    private const int HighGcCollectionThreshold = 1000;
    private const double HighGcTimePercentageThreshold = 0.10;

    public override string RuleName => "GC Thrashing Detection";

    public override string Description => "Detects excessive garbage collection that may indicate memory pressure or excessive allocations.";

    public override List<DiagnosticFinding> Analyze(
        CpuUsage cpuUsage,
        MemorySnapshot memorySnapshot,
        GcStats gcStats,
        ThreadPoolStats threadPoolStats)
    {
        var findings = new List<DiagnosticFinding>();

        // Check for high Gen 0 collections
        if (gcStats.Gen0CollectionCount > HighGcCollectionThreshold)
        {
            findings.Add(new DiagnosticFinding
            {
                RuleName = RuleName,
                Severity = DiagnosticSeverity.Warning,
                Description = $"High Gen 0 garbage collection count ({gcStats.Gen0CollectionCount}) detected. This may indicate excessive allocations.",
                Recommendation = "Review code for unnecessary object allocations. Consider object pooling, using value types, or reducing LINQ allocations.",
                MeasuredValue = gcStats.Gen0CollectionCount.ToString(),
                Threshold = HighGcCollectionThreshold.ToString()
            });
        }

        // Check for high Gen 2 collections (more severe)
        if (gcStats.Gen2CollectionCount > 100)
        {
            findings.Add(new DiagnosticFinding
            {
                RuleName = RuleName,
                Severity = DiagnosticSeverity.Error,
                Description = $"High Gen 2 garbage collection count ({gcStats.Gen2CollectionCount}) detected. Frequent full collections indicate severe memory pressure.",
                Recommendation = "Investigate memory leaks, reduce working set size, or increase available memory.",
                MeasuredValue = gcStats.Gen2CollectionCount.ToString(),
                Threshold = "100"
            });
        }

        return findings;
    }
}
