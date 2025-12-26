using DiagnosticsToolkit.Models;

namespace DiagnosticsToolkit.Analyzer.Rules;

/// <summary>
/// Detects memory pressure and near-capacity conditions.
/// </summary>
public class MemoryPressureRule : DiagnosticRule
{
    private const double CriticalMemoryPercentage = 0.95;
    private const double HighMemoryPercentage = 0.80;

    public override string RuleName => "Memory Pressure Detection";

    public override string Description => "Detects when memory usage is approaching system limits.";

    public override List<DiagnosticFinding> Analyze(
        CpuUsage cpuUsage,
        MemorySnapshot memorySnapshot,
        GcStats gcStats,
        ThreadPoolStats threadPoolStats)
    {
        var findings = new List<DiagnosticFinding>();

        // Only check if total memory is available
        if (memorySnapshot.TotalSystemMemoryBytes == 0)
            return findings;

        var usagePercentage = (double)memorySnapshot.ProcessWorkingSetBytes / memorySnapshot.TotalSystemMemoryBytes;

        if (usagePercentage >= CriticalMemoryPercentage)
        {
            findings.Add(new DiagnosticFinding
            {
                RuleName = RuleName,
                Severity = DiagnosticSeverity.Critical,
                Description = $"Critical memory usage at {(usagePercentage * 100):F1}% ({FormatBytes(memorySnapshot.ProcessWorkingSetBytes)} of {FormatBytes(memorySnapshot.TotalSystemMemoryBytes)}).",
                Recommendation = "Immediately investigate memory leaks, reduce caching, or scale out to more instances.",
                MeasuredValue = $"{(usagePercentage * 100):F1}%",
                Threshold = $"{(CriticalMemoryPercentage * 100):F0}%"
            });
        }
        else if (usagePercentage >= HighMemoryPercentage)
        {
            findings.Add(new DiagnosticFinding
            {
                RuleName = RuleName,
                Severity = DiagnosticSeverity.Error,
                Description = $"High memory usage at {(usagePercentage * 100):F1}% ({FormatBytes(memorySnapshot.ProcessWorkingSetBytes)} of {FormatBytes(memorySnapshot.TotalSystemMemoryBytes)}).",
                Recommendation = "Review memory usage patterns, implement memory limits, or add memory monitoring.",
                MeasuredValue = $"{(usagePercentage * 100):F1}%",
                Threshold = $"{(HighMemoryPercentage * 100):F0}%"
            });
        }

        return findings;
    }

    private static string FormatBytes(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (bytes >= GB)
            return $"{(double)bytes / GB:F2} GB";
        if (bytes >= MB)
            return $"{(double)bytes / MB:F2} MB";
        if (bytes >= KB)
            return $"{(double)bytes / KB:F2} KB";
        return $"{bytes} B";
    }
}
