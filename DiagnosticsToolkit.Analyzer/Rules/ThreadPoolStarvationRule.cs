using DiagnosticsToolkit.Models;

namespace DiagnosticsToolkit.Analyzer.Rules;

/// <summary>
/// Detects thread pool starvation which can cause deadlocks and timeouts.
/// </summary>
public class ThreadPoolStarvationRule : DiagnosticRule
{
    public override string RuleName => "Thread Pool Starvation Detection";

    public override string Description => "Detects when thread pool availability is critically low, which may cause work item queuing and timeouts.";

    public override List<DiagnosticFinding> Analyze(
        CpuUsage cpuUsage,
        MemorySnapshot memorySnapshot,
        GcStats gcStats,
        ThreadPoolStats threadPoolStats)
    {
        var findings = new List<DiagnosticFinding>();

        // High pending work items without enough threads
        if (threadPoolStats.QueuedWorkItemCount > 0 && threadPoolStats.AvailableWorkerThreads < 2)
        {
            findings.Add(new DiagnosticFinding
            {
                RuleName = RuleName,
                Severity = DiagnosticSeverity.Critical,
                Description = $"Critical thread pool starvation: {threadPoolStats.QueuedWorkItemCount} queued work items with only {threadPoolStats.AvailableWorkerThreads} available threads.",
                Recommendation = "Reduce concurrent work submissions, increase thread pool minimum threads with ThreadPool.GetMinThreads/SetMinThreads, or optimize long-running operations.",
                MeasuredValue = $"Queued: {threadPoolStats.QueuedWorkItemCount}, Available: {threadPoolStats.AvailableWorkerThreads}",
                Threshold = "Available threads should be >= 2"
            });
        }

        // Warning: Significant pending work items
        else if (threadPoolStats.QueuedWorkItemCount > 10)
        {
            findings.Add(new DiagnosticFinding
            {
                RuleName = RuleName,
                Severity = DiagnosticSeverity.Warning,
                Description = $"Thread pool has {threadPoolStats.QueuedWorkItemCount} queued work items. This may cause latency spikes.",
                Recommendation = "Monitor for blocking operations, consider batching work, or increase thread pool size.",
                MeasuredValue = threadPoolStats.QueuedWorkItemCount.ToString(),
                Threshold = "10"
            });
        }

        return findings;
    }
}
