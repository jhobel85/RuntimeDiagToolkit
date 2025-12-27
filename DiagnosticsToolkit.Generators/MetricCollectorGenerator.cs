using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace DiagnosticsToolkit.Generators;

/// <summary>
/// Roslyn source generator for [MetricCollector] attribute.
/// Generates execution metric helpers for decorated methods.
/// </summary>
[Generator]
public class MetricCollectorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Emit the attribute definition
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("MetricCollectorAttribute.g.cs", GetAttributeSource());
        });

        // Find methods with [MetricCollector] attribute
        var methodsWithAttr = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is MethodDeclarationSyntax m &&
                    m.AttributeLists.Any(al => al.Attributes.Any(a =>
                        a.Name.ToString() == "MetricCollector" ||
                        a.Name.ToString() == "DiagnosticsToolkit.Generators.MetricCollector")),
                transform: (ctx, _) => ExtractMethod(ctx))
            .Where(m => m != null!);

        // Generate metrics helper for each method
        context.RegisterSourceOutput(methodsWithAttr, (spc, methodInfo) =>
        {
            if (methodInfo != null)
            {
                var source = GenerateMetricsHelper(methodInfo);
                var fileName = $"{methodInfo.ClassName}_{methodInfo.MethodName}_Metrics.g.cs";
                spc.AddSource(fileName, source);
            }
        });
    }

    private static MethodInfo? ExtractMethod(GeneratorSyntaxContext ctx)
    {
        var method = ctx.Node as MethodDeclarationSyntax;
        var parent = method?.Parent as ClassDeclarationSyntax;

        if (parent == null)
            return null;

        var ns = GetNamespace(parent);
        return new MethodInfo
        {
            Namespace = ns,
            ClassName = parent.Identifier.Text,
            MethodName = method!.Identifier.Text,
            ReturnType = method.ReturnType.ToString(),
            IsAsync = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword))
        };
    }

    private static string GetNamespace(SyntaxNode node)
    {
        foreach (var parent in node.Ancestors())
        {
            if (parent is NamespaceDeclarationSyntax ns)
                return ns.Name.ToString();
        }
        return "GeneratedCode";
    }

    private static string GetAttributeSource()
    {
        return @"using System;

namespace DiagnosticsToolkit.Generators;

/// <summary>
/// Marks a method for automatic metric collection code generation.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MetricCollectorAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the metric category name.
    /// </summary>
    public string Category { get; set; } = ""General"";

    /// <summary>
    /// Gets or sets whether to track execution time.
    /// </summary>
    public bool TrackExecutionTime { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track exceptions.
    /// </summary>
    public bool TrackExceptions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track memory allocations.
    /// </summary>
    public bool TrackAllocations { get; set; } = false;
}

/// <summary>
/// Represents execution metrics collected for a method.
/// </summary>
public class MethodMetrics
{
    /// <summary>Gets the method name.</summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>Gets the total number of calls.</summary>
    public int CallCount { get; set; }

    /// <summary>Gets total execution time in milliseconds.</summary>
    public long TotalExecutionTimeMs { get; set; }

    /// <summary>Gets average execution time in milliseconds.</summary>
    public decimal AverageExecutionTimeMs { get; set; }

    /// <summary>Gets the number of exceptions.</summary>
    public int ExceptionCount { get; set; }
}
";
    }

    private static string GenerateMetricsHelper(MethodInfo method)
    {
        var sb = new StringBuilder();
        var ns = method.Namespace != "GeneratedCode" ? method.Namespace : "DiagnosticsToolkit.Generators.Sample";

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Diagnostics;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using DiagnosticsToolkit.Generators;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Auto-generated metrics helper for {method.ClassName}.{method.MethodName}");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"internal static class {method.ClassName}_{method.MethodName}_Metrics");
        sb.AppendLine("{");
        sb.AppendLine("    private static int _callCount = 0;");
        sb.AppendLine("    private static long _totalTimeMs = 0;");
        sb.AppendLine("    private static int _exceptionCount = 0;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Gets collected metrics.</summary>");
        sb.AppendLine("    public static MethodMetrics GetMetrics() => new()");
        sb.AppendLine("    {");
        sb.AppendLine($"        MethodName = \"{method.ClassName}.{method.MethodName}\",");
        sb.AppendLine("        CallCount = _callCount,");
        sb.AppendLine("        TotalExecutionTimeMs = _totalTimeMs,");
        sb.AppendLine("        AverageExecutionTimeMs = _callCount > 0 ? (decimal)_totalTimeMs / _callCount : 0,");
        sb.AppendLine("        ExceptionCount = _exceptionCount");
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Records successful execution.</summary>");
        sb.AppendLine("    internal static void RecordSuccess(long elapsedMs)");
        sb.AppendLine("    {");
        sb.AppendLine("        System.Threading.Interlocked.Increment(ref _callCount);");
        sb.AppendLine("        System.Threading.Interlocked.Add(ref _totalTimeMs, elapsedMs);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Records exception.</summary>");
        sb.AppendLine("    internal static void RecordException()");
        sb.AppendLine("    {");
        sb.AppendLine("        System.Threading.Interlocked.Increment(ref _exceptionCount);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Wraps a synchronous action with timing and exception tracking.</summary>");
        sb.AppendLine("    public static void Measure(Action action)");
        sb.AppendLine("    {");
        sb.AppendLine("        var stopwatch = Stopwatch.StartNew();");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            action();");
        sb.AppendLine("            stopwatch.Stop();");
        sb.AppendLine("            RecordSuccess(stopwatch.ElapsedMilliseconds);");
        sb.AppendLine("        }");
        sb.AppendLine("        catch");
        sb.AppendLine("        {");
        sb.AppendLine("            stopwatch.Stop();");
        sb.AppendLine("            RecordException();");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Wraps a synchronous function with timing and exception tracking.</summary>");
        sb.AppendLine("    public static T Measure<T>(Func<T> func)");
        sb.AppendLine("    {");
        sb.AppendLine("        var stopwatch = Stopwatch.StartNew();");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = func();");
        sb.AppendLine("            stopwatch.Stop();");
        sb.AppendLine("            RecordSuccess(stopwatch.ElapsedMilliseconds);");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch");
        sb.AppendLine("        {");
        sb.AppendLine("            stopwatch.Stop();");
        sb.AppendLine("            RecordException();");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Wraps an asynchronous function (Task) with timing and exception tracking.</summary>");
        sb.AppendLine("    public static async Task MeasureAsync(Func<Task> func)");
        sb.AppendLine("    {");
        sb.AppendLine("        var stopwatch = Stopwatch.StartNew();");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            await func().ConfigureAwait(false);");
        sb.AppendLine("            stopwatch.Stop();");
        sb.AppendLine("            RecordSuccess(stopwatch.ElapsedMilliseconds);");
        sb.AppendLine("        }");
        sb.AppendLine("        catch");
        sb.AppendLine("        {");
        sb.AppendLine("            stopwatch.Stop();");
        sb.AppendLine("            RecordException();");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Wraps an asynchronous function (Task<T>) with timing and exception tracking.</summary>");
        sb.AppendLine("    public static async Task<T> MeasureAsync<T>(Func<Task<T>> func)");
        sb.AppendLine("    {");
        sb.AppendLine("        var stopwatch = Stopwatch.StartNew();");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = await func().ConfigureAwait(false);");
        sb.AppendLine("            stopwatch.Stop();");
        sb.AppendLine("            RecordSuccess(stopwatch.ElapsedMilliseconds);");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch");
        sb.AppendLine("        {");
        sb.AppendLine("            stopwatch.Stop();");
        sb.AppendLine("            RecordException();");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        // Per-method convenience overloads to hide helper type name at call sites - syntactically nicer
        var rt = method.ReturnType.Trim();
        if (method.IsAsync)
        {
            if (rt.StartsWith("Task<"))
            {
                var inner = rt.Substring(5, rt.Length - 6);
                sb.AppendLine($"    public static Task<{inner}> Measure{method.MethodName}Async(Func<Task<{inner}>> func) => MeasureAsync(func);");
            }
            else
            {
                sb.AppendLine($"    public static Task Measure{method.MethodName}Async(Func<Task> func) => MeasureAsync(func);");
            }
        }
        else
        {
            if (rt == "void")
            {
                sb.AppendLine($"    public static void Measure{method.MethodName}(Action action) => Measure(action);");
            }
            else
            {
                sb.AppendLine($"    public static {rt} Measure{method.MethodName}(Func<{rt}> func) => Measure(func);");
            }
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    private sealed class MethodInfo
    {
        public string Namespace { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public bool IsAsync { get; set; }
    }
}
