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
";
    }

    private static string GenerateMetricsHelper(MethodInfo method)
    {
        var sb = new StringBuilder();
        var ns = method.Namespace != "GeneratedCode" ? method.Namespace : "DiagnosticsToolkit.Generators.Sample";

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
