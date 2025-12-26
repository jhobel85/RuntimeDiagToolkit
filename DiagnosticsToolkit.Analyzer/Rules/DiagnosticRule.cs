using DiagnosticsToolkit.Models;

namespace DiagnosticsToolkit.Analyzer.Rules;

/// <summary>
/// Base class for diagnostic rules that analyze metrics for issues.
/// </summary>
public abstract class DiagnosticRule
{
    /// <summary>Gets the name of this rule.</summary>
    public abstract string RuleName { get; }

    /// <summary>Gets the description of what this rule checks.</summary>
    public abstract string Description { get; }

    /// <summary>Analyzes metrics and returns any findings.</summary>
    /// <param name="cpuUsage">Current CPU usage metrics.</param>
    /// <param name="memorySnapshot">Current memory metrics.</param>
    /// <param name="gcStats">Current GC statistics.</param>
    /// <param name="threadPoolStats">Current thread pool statistics.</param>
    /// <returns>List of diagnostic findings, empty if no issues detected.</returns>
    public abstract List<DiagnosticFinding> Analyze(
        CpuUsage cpuUsage,
        MemorySnapshot memorySnapshot,
        GcStats gcStats,
        ThreadPoolStats threadPoolStats);
}
