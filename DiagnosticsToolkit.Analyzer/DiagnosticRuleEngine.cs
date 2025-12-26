using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Analyzer.Rules;

namespace DiagnosticsToolkit.Analyzer;

/// <summary>
/// Engine that applies diagnostic rules to runtime metrics to identify performance issues.
/// </summary>
public class DiagnosticRuleEngine
{
    private readonly List<DiagnosticRule> _rules = new()
    {
        new GcThresholdRule(),
        new ThreadPoolStarvationRule(),
        new MemoryPressureRule(),
        new CpuUtilizationRule()
    };

    /// <summary>
    /// Analyzes metrics using all registered rules.
    /// </summary>
    /// <param name="cpuUsage">Current CPU usage metrics.</param>
    /// <param name="memorySnapshot">Current memory metrics.</param>
    /// <param name="gcStats">Current GC statistics.</param>
    /// <param name="threadPoolStats">Current thread pool statistics.</param>
    /// <returns>Diagnostic report with all findings.</returns>
    public DiagnosticReport Analyze(
        CpuUsage cpuUsage,
        MemorySnapshot memorySnapshot,
        GcStats gcStats,
        ThreadPoolStats threadPoolStats)
    {
        var report = new DiagnosticReport();

        foreach (var rule in _rules)
        {
            var findings = rule.Analyze(cpuUsage, memorySnapshot, gcStats, threadPoolStats);
            report.Findings.AddRange(findings);
        }

        // Sort findings by severity (Critical first, then Error, Warning, Info)
        report.Findings.Sort((a, b) => b.Severity.CompareTo(a.Severity));

        return report;
    }

    /// <summary>
    /// Adds a custom diagnostic rule to the engine.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    public void AddRule(DiagnosticRule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        _rules.Add(rule);
    }

    /// <summary>
    /// Gets all registered rules.
    /// </summary>
    public IReadOnlyList<DiagnosticRule> Rules => _rules.AsReadOnly();
}
