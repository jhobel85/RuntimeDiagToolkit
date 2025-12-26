# DiagnosticsToolkit.Cli

## Overview

**diagnostics-ai** is a command-line tool for analyzing runtime metrics snapshots and generating comprehensive performance diagnostic reports. It uses the DiagnosticsToolkit.Analyzer engine to detect performance issues and provide actionable recommendations.

## Installation

```bash
# Build the CLI
dotnet build DiagnosticsToolkit.Cli/DiagnosticsToolkit.Cli.csproj -c Release

# Run it
./DiagnosticsToolkit.Cli/bin/Release/net8.0/diagnostics-ai
```

## Usage

### Basic Analysis

Analyze a metrics JSON file and print results to console:

```bash
diagnostics-ai analyze --input metrics.json
```

### Save Report to File

```bash
diagnostics-ai analyze -i metrics.json -o report.txt
```

### JSON Output Format

```bash
diagnostics-ai analyze -i metrics.json -o report.json -f json
```

### HTML Report

```bash
diagnostics-ai analyze -i metrics.json -o report.html -f html
```

## Metrics JSON Format

The input JSON file should contain a snapshot of runtime metrics with this structure:

```json
{
  "cpuUsage": {
    "percentageUsed": 65.5,
    "totalProcessorTimeMs": 45200,
    "userModeTimeMs": 32100,
    "privilegedModeTimeMs": 13100,
    "collectedAt": "2025-12-26T14:30:00Z"
  },
  "memorySnapshot": {
    "totalSystemMemoryBytes": 17179869184,
    "availableSystemMemoryBytes": 4294967296,
    "processWorkingSetBytes": 512000000,
    "processPrivateMemoryBytes": 384000000,
    "managedHeapBytes": 256000000,
    "processVirtualMemoryBytes": 1024000000,
    "memoryPressurePercentage": 45,
    "collectedAt": "2025-12-26T14:30:00Z"
  },
  "gcStats": {
    "gen0CollectionCount": 850,
    "gen1CollectionCount": 45,
    "gen2CollectionCount": 8,
    "totalGcPauseMsPercentage": 2.5,
    "heapFragmentationPercentage": 12.3,
    "totalAllocatedBytes": 2147483648,
    "isGcConcurrentEnabled": true,
    "collectedAt": "2025-12-26T14:30:00Z"
  },
  "threadPoolStats": {
    "workerThreadCount": 32,
    "availableWorkerThreads": 28,
    "ioThreadCount": 16,
    "availableIoThreads": 14,
    "queuedWorkItemCount": 2,
    "completedWorkItemCount": 125000,
    "minWorkerThreads": 8,
    "maxWorkerThreads": 128,
    "minIoThreads": 4,
    "maxIoThreads": 64,
    "collectedAt": "2025-12-26T14:30:00Z"
  }
}
```

## Commands

### analyze
Analyze metrics from a JSON file and generate a diagnostic report.

**Options:**
- `-i, --input` (required) - Path to JSON file containing metrics snapshot
- `-o, --output` (optional) - Path to write the analysis report (defaults to console)
- `-f, --format` (optional) - Output format: `text` (default), `json`, or `html`

**Examples:**
```bash
# Console output (text format)
diagnostics-ai analyze -i metrics.json

# Save as text file
diagnostics-ai analyze -i metrics.json -o report.txt -f text

# Save as JSON
diagnostics-ai analyze -i metrics.json -o report.json -f json

# Save as HTML (for viewing in browser)
diagnostics-ai analyze -i metrics.json -o report.html -f html
```

### version
Display version information.

```bash
diagnostics-ai version
```

## Output Formats

### Text Format (Default)
Human-readable report with emojis and clear sections. Best for quick analysis in terminal.

```
═══════════════════════════════════════════════════════════
         DIAGNOSTICS REPORT - PERFORMANCE ANALYSIS
═══════════════════════════════════════════════════════════

Generated: 2025-12-26 14:30:00 UTC
Health Status: ✅ Good

Summary:
  🔴 Critical: 0
  🟠 Errors:   0
  🟡 Warnings: 1
  ℹ️  Info:     0

Findings:
🟡 [Warning] CPU Utilization Detection
   Description: Elevated CPU usage at 65.5%. Monitor for sustained high usage patterns.
   ...
```

### JSON Format
Structured output for programmatic processing and integration.

```json
{
  "generatedAt": "2025-12-26T14:30:00Z",
  "findings": [
    {
      "ruleName": "CPU Utilization Detection",
      "severity": "Warning",
      "description": "Elevated CPU usage...",
      "recommendation": "Monitor trends...",
      "measuredValue": "65.5%",
      "threshold": "70%"
    }
  ],
  "criticalCount": 0,
  "errorCount": 0,
  "warningCount": 1,
  "infoCount": 0,
  "healthStatus": "Fair"
}
```

### HTML Format
Beautiful styled report for sharing and archiving. Open in any web browser.

Features:
- Color-coded severity levels
- Summary statistics cards
- Detailed findings with recommendations
- Responsive design
- Print-friendly styling

## Integration with ASP.NET Core

Collect metrics from a running ASP.NET Core application and analyze:

```csharp
// In your ASP.NET Core app
using DiagnosticsToolkit;
using DiagnosticsToolkit.Analyzer;

var provider = RuntimeMetricsProviderFactory.Create();
var cpu = await provider.GetCpuUsageAsync();
var memory = await provider.GetMemorySnapshotAsync();
var gc = await provider.GetGcStatsAsync();
var threadPool = await provider.GetThreadPoolStatsAsync();

// Save to JSON
var snapshot = new
{
    cpuUsage = cpu,
    memorySnapshot = memory,
    gcStats = gc,
    threadPoolStats = threadPool
};

var json = System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
await System.IO.File.WriteAllTextAsync("metrics.json", json);

// Then analyze via CLI
// diagnostics-ai analyze -i metrics.json -o report.html -f html
```

## Exit Codes

- `0` - Analysis completed successfully
- `1` - Error during analysis (missing file, invalid JSON, etc.)

## Performance

The CLI is optimized for:
- Fast JSON deserialization
- Quick rule analysis (all rules run in <1ms)
- Low memory footprint
- Suitable for CI/CD pipelines

## Troubleshooting

**"Input file not found"**
- Verify the path to your metrics JSON file is correct
- Use absolute paths if relative paths don't work

**"Could not deserialize metrics from JSON file"**
- Validate your JSON syntax (use a JSON validator)
- Ensure all required fields are present (see format above)
- Check property names are correctly cased

**Command not found**
- Ensure the CLI binary is in your PATH or call with full path
- On Windows: `.\diagnostics-ai.exe analyze -i metrics.json`
- On macOS/Linux: `./diagnostics-ai analyze -i metrics.json`

## Future Enhancements

- Real-time monitoring mode (continuous metric collection)
- Comparison reports (before/after analysis)
- Threshold customization via config files
- Integration with APM tools (Application Insights, Datadog, etc.)
- Auto-remediation suggestions
- Metrics history and trend analysis
