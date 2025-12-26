using DiagnosticsToolkit.Models;

namespace DiagnosticsToolkit.Analyzer.Rules;

/// <summary>
/// Detects high CPU utilization that may indicate performance issues or suboptimal resource allocation.
/// </summary>
public class CpuUtilizationRule : DiagnosticRule
{
    private const double CriticalCpuPercentage = 0.95;
    private const double HighCpuPercentage = 0.80;
    private const double WarningCpuPercentage = 0.70;

    public override string RuleName => "CPU Utilization Detection";

    public override string Description => "Detects high CPU utilization that may indicate processing bottlenecks or insufficient resources.";

    public override List<DiagnosticFinding> Analyze(
        CpuUsage cpuUsage,
        MemorySnapshot memorySnapshot,
        GcStats gcStats,
        ThreadPoolStats threadPoolStats)
    {
        var findings = new List<DiagnosticFinding>();

        if (cpuUsage.PercentageUsed >= CriticalCpuPercentage * 100)  // PercentageUsed is 0-100, so multiply threshold
        {
            findings.Add(new DiagnosticFinding
            {
                RuleName = RuleName,
                Severity = DiagnosticSeverity.Critical,
                Description = $"Critical CPU usage at {cpuUsage.PercentageUsed:F1}%. The process is consuming nearly all available CPU time.",
                Recommendation = "Profile code for CPU-intensive operations, optimize algorithms, parallelize workloads, or add more compute resources.",
                MeasuredValue = $"{cpuUsage.PercentageUsed:F1}%",
                Threshold = $"{(CriticalCpuPercentage * 100):F0}%"
            });
        }
        else if (cpuUsage.PercentageUsed >= HighCpuPercentage * 100)
        {
            findings.Add(new DiagnosticFinding
            {
                RuleName = RuleName,
                Severity = DiagnosticSeverity.Error,
                Description = $"High CPU usage at {cpuUsage.PercentageUsed:F1}%. The process is using most available CPU resources.",
                Recommendation = "Use profiling tools to identify hot paths, consider caching, lazy loading, or async operations.",
                MeasuredValue = $"{cpuUsage.PercentageUsed:F1}%",
                Threshold = $"{(HighCpuPercentage * 100):F0}%"
            });
        }
        else if (cpuUsage.PercentageUsed >= WarningCpuPercentage * 100)
        {
            findings.Add(new DiagnosticFinding
            {
                RuleName = RuleName,
                Severity = DiagnosticSeverity.Warning,
                Description = $"Elevated CPU usage at {cpuUsage.PercentageUsed:F1}%. Monitor for sustained high usage patterns.",
                Recommendation = "Monitor trends, profile for optimization opportunities, and consider load testing.",
                MeasuredValue = $"{cpuUsage.PercentageUsed:F1}%",
                Threshold = $"{(WarningCpuPercentage * 100):F0}%"
            });
        }

        return findings;
    }
}
