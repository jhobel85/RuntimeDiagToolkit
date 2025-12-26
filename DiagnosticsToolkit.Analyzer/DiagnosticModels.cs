namespace DiagnosticsToolkit.Analyzer;

/// <summary>
/// Severity level for diagnostic findings.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational - no action needed.</summary>
    Info,
    
    /// <summary>Warning - might become an issue.</summary>
    Warning,
    
    /// <summary>Error - likely causing performance issues.</summary>
    Error,
    
    /// <summary>Critical - immediate action recommended.</summary>
    Critical
}

/// <summary>
/// Represents a diagnostic finding from the analyzer.
/// </summary>
public class DiagnosticFinding
{
    /// <summary>Gets or sets the rule name.</summary>
    public required string RuleName { get; set; }

    /// <summary>Gets or sets the severity level.</summary>
    public DiagnosticSeverity Severity { get; set; }

    /// <summary>Gets or sets the description of the issue.</summary>
    public required string Description { get; set; }

    /// <summary>Gets or sets the recommended action.</summary>
    public required string Recommendation { get; set; }

    /// <summary>Gets or sets the measured value that triggered the finding.</summary>
    public string? MeasuredValue { get; set; }

    /// <summary>Gets or sets the threshold that was exceeded.</summary>
    public string? Threshold { get; set; }
}

/// <summary>
/// Complete diagnostic report with all findings and summary.
/// </summary>
public class DiagnosticReport
{
    /// <summary>Gets or sets the timestamp when the report was generated.</summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the list of findings.</summary>
    public List<DiagnosticFinding> Findings { get; set; } = new();

    /// <summary>Gets the count of findings by severity.</summary>
    public int CriticalCount => Findings.Count(f => f.Severity == DiagnosticSeverity.Critical);
    
    /// <summary>Gets the count of error findings.</summary>
    public int ErrorCount => Findings.Count(f => f.Severity == DiagnosticSeverity.Error);
    
    /// <summary>Gets the count of warning findings.</summary>
    public int WarningCount => Findings.Count(f => f.Severity == DiagnosticSeverity.Warning);
    
    /// <summary>Gets the count of info findings.</summary>
    public int InfoCount => Findings.Count(f => f.Severity == DiagnosticSeverity.Info);

    /// <summary>Gets overall health status.</summary>
    public string HealthStatus =>
        CriticalCount > 0 ? "Critical" :
        ErrorCount > 0 ? "Poor" :
        WarningCount > 0 ? "Fair" :
        "Good";
}
