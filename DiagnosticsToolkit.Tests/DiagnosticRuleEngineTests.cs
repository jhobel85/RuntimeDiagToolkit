using Xunit;
using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Analyzer;
using DiagnosticsToolkit.Analyzer.Rules;

namespace DiagnosticsToolkit.Tests;

public class DiagnosticRuleEngineTests
{
    [Fact]
    public void Analyze_WithNormalMetrics_ReturnsNoFindings()
    {
        // Arrange
        var engine = new DiagnosticRuleEngine();
        var cpu = new CpuUsage { PercentageUsed = 25.0, CollectedAt = DateTimeOffset.UtcNow };
        var memory = new MemorySnapshot { ProcessWorkingSetBytes = 500_000_000, TotalSystemMemoryBytes = 8_000_000_000, CollectedAt = DateTimeOffset.UtcNow };
        var gc = new GcStats { Gen0CollectionCount = 100, Gen1CollectionCount = 50, Gen2CollectionCount = 10, CollectedAt = DateTimeOffset.UtcNow };
        var threadPool = new ThreadPoolStats { AvailableWorkerThreads = 50, QueuedWorkItemCount = 0, CollectedAt = DateTimeOffset.UtcNow };

        // Act
        var report = engine.Analyze(cpu, memory, gc, threadPool);

        // Assert
        Assert.NotNull(report);
        Assert.Empty(report.Findings);
        Assert.Equal("Good", report.HealthStatus);
    }

    [Fact]
    public void Analyze_WithHighGcGen0_ReturnsWarning()
    {
        // Arrange
        var engine = new DiagnosticRuleEngine();
        var cpu = new CpuUsage { PercentageUsed = 25.0, CollectedAt = DateTimeOffset.UtcNow };
        var memory = new MemorySnapshot { ProcessWorkingSetBytes = 500_000_000, TotalSystemMemoryBytes = 8_000_000_000, CollectedAt = DateTimeOffset.UtcNow };
        var gc = new GcStats { Gen0CollectionCount = 2000, Gen1CollectionCount = 50, Gen2CollectionCount = 10, CollectedAt = DateTimeOffset.UtcNow };
        var threadPool = new ThreadPoolStats { AvailableWorkerThreads = 50, QueuedWorkItemCount = 0, CollectedAt = DateTimeOffset.UtcNow };

        // Act
        var report = engine.Analyze(cpu, memory, gc, threadPool);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.Findings);
        var finding = Assert.Single(report.Findings);
        Assert.Equal(DiagnosticSeverity.Warning, finding.Severity);
        Assert.Contains("Gen 0", finding.Description);
    }

    [Fact]
    public void Analyze_WithHighGcGen2_ReturnsError()
    {
        // Arrange
        var engine = new DiagnosticRuleEngine();
        var cpu = new CpuUsage { PercentageUsed = 25.0, CollectedAt = DateTimeOffset.UtcNow };
        var memory = new MemorySnapshot { ProcessWorkingSetBytes = 500_000_000, TotalSystemMemoryBytes = 8_000_000_000, CollectedAt = DateTimeOffset.UtcNow };
        var gc = new GcStats { Gen0CollectionCount = 100, Gen1CollectionCount = 50, Gen2CollectionCount = 150, CollectedAt = DateTimeOffset.UtcNow };
        var threadPool = new ThreadPoolStats { AvailableWorkerThreads = 50, QueuedWorkItemCount = 0, CollectedAt = DateTimeOffset.UtcNow };

        // Act
        var report = engine.Analyze(cpu, memory, gc, threadPool);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.Findings);
        var finding = Assert.Single(report.Findings);
        Assert.Equal(DiagnosticSeverity.Error, finding.Severity);
        Assert.Contains("Gen 2", finding.Description);
    }

    [Fact]
    public void Analyze_WithThreadPoolStarvation_ReturnsCritical()
    {
        // Arrange
        var engine = new DiagnosticRuleEngine();
        var cpu = new CpuUsage { PercentageUsed = 25.0, CollectedAt = DateTimeOffset.UtcNow };
        var memory = new MemorySnapshot { ProcessWorkingSetBytes = 500_000_000, TotalSystemMemoryBytes = 8_000_000_000, CollectedAt = DateTimeOffset.UtcNow };
        var gc = new GcStats { Gen0CollectionCount = 100, Gen1CollectionCount = 50, Gen2CollectionCount = 10, CollectedAt = DateTimeOffset.UtcNow };
        var threadPool = new ThreadPoolStats { AvailableWorkerThreads = 1, QueuedWorkItemCount = 50, CollectedAt = DateTimeOffset.UtcNow };

        // Act
        var report = engine.Analyze(cpu, memory, gc, threadPool);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.Findings);
        var finding = Assert.Single(report.Findings);
        Assert.Equal(DiagnosticSeverity.Critical, finding.Severity);
        Assert.Contains("starvation", finding.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_WithHighMemoryUsage_ReturnsError()
    {
        // Arrange
        var engine = new DiagnosticRuleEngine();
        var cpu = new CpuUsage { PercentageUsed = 25.0, CollectedAt = DateTimeOffset.UtcNow };
        var memory = new MemorySnapshot { ProcessWorkingSetBytes = 7_200_000_000, TotalSystemMemoryBytes = 8_000_000_000, CollectedAt = DateTimeOffset.UtcNow }; // 90%
        var gc = new GcStats { Gen0CollectionCount = 100, Gen1CollectionCount = 50, Gen2CollectionCount = 10, CollectedAt = DateTimeOffset.UtcNow };
        var threadPool = new ThreadPoolStats { AvailableWorkerThreads = 50, QueuedWorkItemCount = 0, CollectedAt = DateTimeOffset.UtcNow };

        // Act
        var report = engine.Analyze(cpu, memory, gc, threadPool);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.Findings);
        var finding = Assert.Single(report.Findings);
        Assert.Equal(DiagnosticSeverity.Error, finding.Severity);
        Assert.Contains("High memory", finding.Description);
    }

    [Fact]
    public void Analyze_WithCriticalMemoryUsage_ReturnsCritical()
    {
        // Arrange
        var engine = new DiagnosticRuleEngine();
        var cpu = new CpuUsage { PercentageUsed = 25.0, CollectedAt = DateTimeOffset.UtcNow };
        var memory = new MemorySnapshot { ProcessWorkingSetBytes = 7_700_000_000, TotalSystemMemoryBytes = 8_000_000_000, CollectedAt = DateTimeOffset.UtcNow }; // 96.25%
        var gc = new GcStats { Gen0CollectionCount = 100, Gen1CollectionCount = 50, Gen2CollectionCount = 10, CollectedAt = DateTimeOffset.UtcNow };
        var threadPool = new ThreadPoolStats { AvailableWorkerThreads = 50, QueuedWorkItemCount = 0, CollectedAt = DateTimeOffset.UtcNow };

        // Act
        var report = engine.Analyze(cpu, memory, gc, threadPool);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.Findings);
        var finding = Assert.Single(report.Findings);
        Assert.Equal(DiagnosticSeverity.Critical, finding.Severity);
        Assert.Contains("Critical memory", finding.Description);
    }

    [Fact]
    public void Analyze_WithHighCpuUsage_ReturnsError()
    {
        // Arrange
        var engine = new DiagnosticRuleEngine();
        var cpu = new CpuUsage { PercentageUsed = 85.0, CollectedAt = DateTimeOffset.UtcNow };
        var memory = new MemorySnapshot { ProcessWorkingSetBytes = 500_000_000, TotalSystemMemoryBytes = 8_000_000_000, CollectedAt = DateTimeOffset.UtcNow };
        var gc = new GcStats { Gen0CollectionCount = 100, Gen1CollectionCount = 50, Gen2CollectionCount = 10, CollectedAt = DateTimeOffset.UtcNow };
        var threadPool = new ThreadPoolStats { AvailableWorkerThreads = 50, QueuedWorkItemCount = 0, CollectedAt = DateTimeOffset.UtcNow };

        // Act
        var report = engine.Analyze(cpu, memory, gc, threadPool);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.Findings);
        var finding = Assert.Single(report.Findings);
        Assert.Equal(DiagnosticSeverity.Error, finding.Severity);
        Assert.Contains("High CPU", finding.Description);
    }

    [Fact]
    public void Analyze_WithMultipleIssues_ReturnsSortedByServerity()
    {
        // Arrange
        var engine = new DiagnosticRuleEngine();
        var cpu = new CpuUsage { PercentageUsed = 96.0, CollectedAt = DateTimeOffset.UtcNow }; // Critical
        var memory = new MemorySnapshot { ProcessWorkingSetBytes = 7_700_000_000, TotalSystemMemoryBytes = 8_000_000_000, CollectedAt = DateTimeOffset.UtcNow }; // Critical
        var gc = new GcStats { Gen0CollectionCount = 2000, Gen1CollectionCount = 50, Gen2CollectionCount = 150, CollectedAt = DateTimeOffset.UtcNow }; // Warning + Error
        var threadPool = new ThreadPoolStats { AvailableWorkerThreads = 50, QueuedWorkItemCount = 5, CollectedAt = DateTimeOffset.UtcNow };

        // Act
        var report = engine.Analyze(cpu, memory, gc, threadPool);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.Findings);
        // Should have multiple findings
        Assert.True(report.Findings.Count > 1);
        // First finding should be Critical
        Assert.Equal(DiagnosticSeverity.Critical, report.Findings[0].Severity);
    }

    [Fact]
    public void ReportHealthStatus_WithCriticalFindings_ShowsCritical()
    {
        // Arrange
        var report = new DiagnosticReport
        {
            Findings = new()
            {
                new DiagnosticFinding { RuleName = "Test", Severity = DiagnosticSeverity.Critical, Description = "Test", Recommendation = "Test" }
            }
        };

        // Act
        var status = report.HealthStatus;

        // Assert
        Assert.Equal("Critical", status);
    }

    [Fact]
    public void ReportHealthStatus_WithOnlyWarnings_ShowsFair()
    {
        // Arrange
        var report = new DiagnosticReport
        {
            Findings = new()
            {
                new DiagnosticFinding { RuleName = "Test", Severity = DiagnosticSeverity.Warning, Description = "Test", Recommendation = "Test" }
            }
        };

        // Act
        var status = report.HealthStatus;

        // Assert
        Assert.Equal("Fair", status);
    }
}
