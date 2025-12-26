using System.Text.Json;
using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Analyzer;

// Simple argument parsing
var command = args.Length > 0 ? args[0] : "help";

if (command == "help" || command == "-h" || command == "--help")
{
    PrintHelp();
    return 0;
}

if (command == "version" || command == "-v" || command == "--version")
{
    Console.WriteLine("Diagnostics AI v1.0.0");
    Console.WriteLine("Runtime Diagnostics Toolkit - AI-powered performance analysis");
    return 0;
}

if (command == "analyze")
{
    return await HandleAnalyze(args.Skip(1).ToArray());
}

Console.Error.WriteLine($"Unknown command: {command}");
PrintHelp();
return 1;

async Task<int> HandleAnalyze(string[] args)
{
    try
    {
        // Parse arguments
        string? inputPath = null;
        string? outputPath = null;
        string format = "text";

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-i" || args[i] == "--input") && i + 1 < args.Length)
                inputPath = args[++i];
            else if ((args[i] == "-o" || args[i] == "--output") && i + 1 < args.Length)
                outputPath = args[++i];
            else if ((args[i] == "-f" || args[i] == "--format") && i + 1 < args.Length)
                format = args[++i];
            else if (args[i] == "-h" || args[i] == "--help")
            {
                PrintAnalyzeHelp();
                return 0;
            }
        }

        if (string.IsNullOrEmpty(inputPath))
        {
            Console.Error.WriteLine("❌ Error: --input is required");
            PrintAnalyzeHelp();
            return 1;
        }

        var inputFile = new FileInfo(inputPath);
        if (!inputFile.Exists)
        {
            Console.Error.WriteLine($"❌ Error: File not found: {inputPath}");
            return 1;
        }

        // Read and parse JSON
        var json = await File.ReadAllTextAsync(inputFile.FullName);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var metricsData = JsonSerializer.Deserialize<MetricsSnapshot>(json, options);

        if (metricsData == null)
        {
            Console.Error.WriteLine("❌ Error: Could not deserialize metrics from JSON file.");
            return 1;
        }

        // Run analyzer
        var engine = new DiagnosticRuleEngine();
        var report = engine.Analyze(
            metricsData.CpuUsage,
            metricsData.MemorySnapshot,
            metricsData.GcStats,
            metricsData.ThreadPoolStats);

        // Output report
        string reportContent = format.ToLower() switch
        {
            "json" => FormatAsJson(report),
            "html" => FormatAsHtml(report),
            "text" or _ => FormatAsText(report)
        };

        if (!string.IsNullOrEmpty(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, reportContent);
            Console.WriteLine($"✅ Report written to: {outputPath}");
        }
        else
        {
            Console.WriteLine(reportContent);
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error: {ex.Message}");
        return 1;
    }
}

void PrintHelp()
{
    Console.WriteLine("Diagnostics AI - Analyze runtime metrics for performance issues");
    Console.WriteLine();
    Console.WriteLine("Usage: diagnostics-ai <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  analyze   Analyze metrics from a JSON file");
    Console.WriteLine("  version   Show version information");
    Console.WriteLine("  help      Show this help message");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -h, --help              Show help message");
    Console.WriteLine("  -v, --version           Show version");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  diagnostics-ai analyze -i metrics.json");
    Console.WriteLine("  diagnostics-ai analyze -i metrics.json -o report.html -f html");
    Console.WriteLine("  diagnostics-ai analyze -i metrics.json -f json > report.json");
}

void PrintAnalyzeHelp()
{
    Console.WriteLine("Analyze metrics from a JSON file and report findings");
    Console.WriteLine();
    Console.WriteLine("Usage: diagnostics-ai analyze [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -i, --input <path>      Input JSON file (required)");
    Console.WriteLine("  -o, --output <path>     Output file (optional, defaults to console)");
    Console.WriteLine("  -f, --format <format>   Output format: text, json, html (default: text)");
    Console.WriteLine("  -h, --help              Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  diagnostics-ai analyze -i metrics.json");
    Console.WriteLine("  diagnostics-ai analyze -i metrics.json -o report.txt");
    Console.WriteLine("  diagnostics-ai analyze -i metrics.json -o report.html -f html");
}

static string FormatAsText(DiagnosticReport report)
{
    var sb = new System.Text.StringBuilder();
    
    sb.AppendLine("═══════════════════════════════════════════════════════════");
    sb.AppendLine("         DIAGNOSTICS REPORT - PERFORMANCE ANALYSIS");
    sb.AppendLine("═══════════════════════════════════════════════════════════");
    sb.AppendLine();
    
    sb.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
    sb.AppendLine($"Health Status: {GetHealthIcon(report.HealthStatus)} {report.HealthStatus}");
    sb.AppendLine();

    sb.AppendLine("Summary:");
    sb.AppendLine($"  🔴 Critical: {report.CriticalCount}");
    sb.AppendLine($"  🟠 Errors:   {report.ErrorCount}");
    sb.AppendLine($"  🟡 Warnings: {report.WarningCount}");
    sb.AppendLine($"  ℹ️  Info:     {report.InfoCount}");
    sb.AppendLine();

    if (report.Findings.Count == 0)
    {
        sb.AppendLine("✅ No issues detected. System is running normally.");
    }
    else
    {
        sb.AppendLine("Findings:");
        sb.AppendLine();

        foreach (var finding in report.Findings)
        {
            var icon = finding.Severity switch
            {
                DiagnosticSeverity.Critical => "🔴",
                DiagnosticSeverity.Error => "🟠",
                DiagnosticSeverity.Warning => "🟡",
                _ => "ℹ️"
            };

            sb.AppendLine($"{icon} [{finding.Severity}] {finding.RuleName}");
            sb.AppendLine($"   Description: {finding.Description}");
            
            if (!string.IsNullOrEmpty(finding.MeasuredValue))
                sb.AppendLine($"   Measured: {finding.MeasuredValue}");
            
            if (!string.IsNullOrEmpty(finding.Threshold))
                sb.AppendLine($"   Threshold: {finding.Threshold}");
            
            sb.AppendLine($"   Action: {finding.Recommendation}");
            sb.AppendLine();
        }
    }

    sb.AppendLine("═══════════════════════════════════════════════════════════");
    
    return sb.ToString();
}

static string FormatAsJson(DiagnosticReport report)
{
    var options = new JsonSerializerOptions { WriteIndented = true };
    return JsonSerializer.Serialize(report, options);
}

static string FormatAsHtml(DiagnosticReport report)
{
    var sb = new System.Text.StringBuilder();
    
    sb.AppendLine("<!DOCTYPE html>");
    sb.AppendLine("<html lang=\"en\">");
    sb.AppendLine("<head>");
    sb.AppendLine("  <meta charset=\"UTF-8\">");
    sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
    sb.AppendLine("  <title>Diagnostics Report</title>");
    sb.AppendLine("  <style>");
    sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 20px; background: #f5f5f5; }");
    sb.AppendLine("    .container { max-width: 1000px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
    sb.AppendLine("    h1 { color: #333; border-bottom: 3px solid #007bff; padding-bottom: 10px; }");
    sb.AppendLine("    .status { display: inline-block; padding: 10px 15px; border-radius: 4px; font-weight: bold; }");
    sb.AppendLine("    .status.good { background: #d4edda; color: #155724; }");
    sb.AppendLine("    .status.fair { background: #fff3cd; color: #856404; }");
    sb.AppendLine("    .status.poor { background: #f8d7da; color: #721c24; }");
    sb.AppendLine("    .status.critical { background: #f5c6cb; color: #721c24; }");
    sb.AppendLine("    .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 15px; margin: 20px 0; }");
    sb.AppendLine("    .stat-card { background: #f8f9fa; padding: 15px; border-radius: 4px; text-align: center; border-left: 4px solid #007bff; }");
    sb.AppendLine("    .stat-value { font-size: 24px; font-weight: bold; }");
    sb.AppendLine("    .stat-label { color: #666; font-size: 14px; margin-top: 5px; }");
    sb.AppendLine("    .critical { border-left-color: #dc3545; }");
    sb.AppendLine("    .error { border-left-color: #fd7e14; }");
    sb.AppendLine("    .warning { border-left-color: #ffc107; }");
    sb.AppendLine("    .info { border-left-color: #17a2b8; }");
    sb.AppendLine("    .finding { background: #f9f9f9; border-left: 4px solid #ddd; padding: 15px; margin: 10px 0; border-radius: 4px; }");
    sb.AppendLine("    .finding.critical { border-left-color: #dc3545; background: #fff5f5; }");
    sb.AppendLine("    .finding.error { border-left-color: #fd7e14; background: #fffaf5; }");
    sb.AppendLine("    .finding.warning { border-left-color: #ffc107; background: #fffef5; }");
    sb.AppendLine("    .finding h3 { margin: 0 0 10px 0; }");
    sb.AppendLine("    .finding-detail { color: #666; font-size: 14px; margin: 5px 0; }");
    sb.AppendLine("    .recommendation { background: #e7f3ff; border-left: 3px solid #2196F3; padding: 10px; margin-top: 10px; border-radius: 3px; }");
    sb.AppendLine("  </style>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body>");
    sb.AppendLine("  <div class=\"container\">");
    sb.AppendLine("    <h1>Diagnostics Report</h1>");
    
    var statusClass = report.HealthStatus.ToLower();
    sb.AppendLine($"    <div class=\"status {statusClass}\">Health: {report.HealthStatus}</div>");
    sb.AppendLine($"    <p style=\"color: #999; margin-top: 10px;\">Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}</p>");
    
    sb.AppendLine("    <h2>Summary</h2>");
    sb.AppendLine("    <div class=\"summary\">");
    sb.AppendLine($"      <div class=\"stat-card critical\"><div class=\"stat-value\">{report.CriticalCount}</div><div class=\"stat-label\">Critical</div></div>");
    sb.AppendLine($"      <div class=\"stat-card error\"><div class=\"stat-value\">{report.ErrorCount}</div><div class=\"stat-label\">Errors</div></div>");
    sb.AppendLine($"      <div class=\"stat-card warning\"><div class=\"stat-value\">{report.WarningCount}</div><div class=\"stat-label\">Warnings</div></div>");
    sb.AppendLine($"      <div class=\"stat-card info\"><div class=\"stat-value\">{report.InfoCount}</div><div class=\"stat-label\">Info</div></div>");
    sb.AppendLine("    </div>");

    if (report.Findings.Count == 0)
    {
        sb.AppendLine("    <div style=\"background: #d4edda; border: 1px solid #c3e6cb; border-radius: 4px; padding: 15px; color: #155724;\">");
        sb.AppendLine("      ✅ No issues detected. System is running normally.");
        sb.AppendLine("    </div>");
    }
    else
    {
        sb.AppendLine("    <h2>Findings</h2>");
        foreach (var finding in report.Findings)
        {
            var severityClass = finding.Severity.ToString().ToLower();
            sb.AppendLine($"    <div class=\"finding {severityClass}\">");
            sb.AppendLine($"      <h3>{finding.RuleName}</h3>");
            sb.AppendLine($"      <p>{finding.Description}</p>");
            
            if (!string.IsNullOrEmpty(finding.MeasuredValue))
                sb.AppendLine($"      <div class=\"finding-detail\"><strong>Measured:</strong> {finding.MeasuredValue}</div>");
            
            if (!string.IsNullOrEmpty(finding.Threshold))
                sb.AppendLine($"      <div class=\"finding-detail\"><strong>Threshold:</strong> {finding.Threshold}</div>");
            
            sb.AppendLine($"      <div class=\"recommendation\"><strong>Action:</strong> {finding.Recommendation}</div>");
            sb.AppendLine("    </div>");
        }
    }

    sb.AppendLine("  </div>");
    sb.AppendLine("</body>");
    sb.AppendLine("</html>");
    
    return sb.ToString();
}

static string GetHealthIcon(string status) => status switch
{
    "Good" => "✅",
    "Fair" => "⚠️",
    "Poor" => "🔶",
    "Critical" => "🔴",
    _ => "❓"
};

/// <summary>Container for deserialized metrics from JSON.</summary>
class MetricsSnapshot
{
    public CpuUsage CpuUsage { get; set; } = default!;
    public MemorySnapshot MemorySnapshot { get; set; } = default!;
    public GcStats GcStats { get; set; } = default!;
    public ThreadPoolStats ThreadPoolStats { get; set; } = default!;
}
