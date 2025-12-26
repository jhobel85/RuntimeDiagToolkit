# DiagnosticsToolkit.Analyzer

## Overview

The **DiagnosticsToolkit.Analyzer** is a rules-based diagnostic engine that analyzes runtime metrics collected by `DiagnosticsToolkit` to detect common performance issues and provide actionable recommendations.

## Features

- **GC Thrashing Detection** - Identifies excessive garbage collection (Gen 0/2 collections)
- **Thread Pool Starvation Detection** - Detects critical thread availability issues and queued work items
- **Memory Pressure Detection** - Tracks memory usage relative to system capacity
- **CPU Utilization Detection** - Monitors process CPU usage patterns with severity levels

## Components

### DiagnosticModels.cs
- `DiagnosticSeverity` - Enum with severity levels (Info, Warning, Error, Critical)
- `DiagnosticFinding` - Represents a single diagnostic finding with measured value, threshold, and recommendation
- `DiagnosticReport` - Complete report with findings, severity counts, and health status

### Rules/
- `DiagnosticRule` - Base class for all diagnostic rules
- `GcThresholdRule` - Analyzes GC collection counts for patterns
- `ThreadPoolStarvationRule` - Monitors thread pool health
- `MemoryPressureRule` - Calculates memory usage percentages relative to system limits
- `CpuUtilizationRule` - Assesses CPU utilization with thresholds

### DiagnosticRuleEngine.cs
Central engine that:
- Registers and manages all diagnostic rules
- Analyzes metrics by applying all rules
- Sorts findings by severity (Critical → Error → Warning → Info)
- Supports custom rule addition via `AddRule()`

## Usage

```csharp
using DiagnosticsToolkit;
using DiagnosticsToolkit.Analyzer;

// Collect metrics
var provider = RuntimeMetricsProviderFactory.Create();
var cpu = await provider.GetCpuUsageAsync();
var memory = await provider.GetMemorySnapshotAsync();
var gc = await provider.GetGcStatsAsync();
var threadPool = await provider.GetThreadPoolStatsAsync();

// Analyze
var engine = new DiagnosticRuleEngine();
var report = engine.Analyze(cpu, memory, gc, threadPool);

// Use results
Console.WriteLine($"Health: {report.HealthStatus}");
foreach (var finding in report.Findings)
{
    Console.WriteLine($"[{finding.Severity}] {finding.Description}");
    Console.WriteLine($"  → {finding.Recommendation}");
}
```

## Severity Levels

- **Critical** - Immediate action required (starvation, critical memory pressure)
- **Error** - Performance impact likely (high GC, high CPU)
- **Warning** - Monitor for patterns (elevated metrics, moderate queued work)
- **Info** - Informational findings

## Extensibility

Add custom rules by extending `DiagnosticRule`:

```csharp
public class CustomRule : DiagnosticRule
{
    public override string RuleName => "My Custom Rule";
    public override string Description => "Detects custom issue";

    public override List<DiagnosticFinding> Analyze(
        CpuUsage cpu, MemorySnapshot memory,
        GcStats gc, ThreadPoolStats threadPool)
    {
        var findings = new List<DiagnosticFinding>();
        // Your logic here
        return findings;
    }
}

var engine = new DiagnosticRuleEngine();
engine.AddRule(new CustomRule());
```

## Testing

The analyzer includes 10 comprehensive unit tests covering:
- Normal metrics (no findings)
- GC threshold violations (Gen 0 and Gen 2)
- Thread pool starvation scenarios
- Memory pressure detection (high and critical)
- CPU utilization levels
- Multiple simultaneous issues with correct severity ordering

Run tests: `dotnet test DiagnosticsToolkit.Tests`

## Built-in Rules

### GcThresholdRule
- **Gen 0**: Triggers warning at >1000 collections (excessive allocations)
- **Gen 2**: Triggers error at >100 collections (severe memory pressure)

### ThreadPoolStarvationRule
- **Critical**: <2 available threads with queued work
- **Warning**: >10 queued work items

### MemoryPressureRule
- **Critical**: >95% system memory usage
- **Error**: >80% system memory usage
- Uses ProcessWorkingSetBytes for process memory tracking

### CpuUtilizationRule
- **Critical**: >95% CPU usage
- **Error**: >80% CPU usage
- **Warning**: >70% CPU usage

## Future Enhancements

- Integration with OpenAI API for AI-powered recommendations
- Persistent findings storage for trend analysis
- Export reports to JSON/HTML formats
- Threshold customization per rule
- Machine learning for anomaly detection


## AI Architecture in diagnostics-ai
Core Component: DiagnosticRuleEngine applies 4 expert-system rules to detect performance issues:

GcThresholdRule — Detects garbage collection thrashing

Warning if Gen 0 collections > 1000
Error if Gen 2 collections > 100
ThreadPoolStarvationRule — Detects thread starvation

Critical if < 2 available worker threads
Warning if > 10 queued work items
MemoryPressureRule — Detects high memory usage

Error if memory usage > 80%
Critical if > 95%
CpuUtilizationRule — Detects CPU saturation

Warning at 70%, Error at 80%, Critical at 95%

The "AI" is domain knowledge encoded as rules, not neural networks — making it fast (< 1ms), deterministic, and explainable.