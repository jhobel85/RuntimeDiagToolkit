using DiagnosticsToolkit.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace DiagnosticsToolkit.Maui.Extensions;

/// <summary>
/// Extension methods to wire DiagnosticsToolkit into a MAUI app.
/// </summary>
public static class MauiDiagnosticsExtensions
{
    /// <summary>
    /// Registers runtime diagnostics services for MAUI apps.
    /// Usage: builder.UseDiagnosticsToolkit();
    /// </summary>
    public static MauiAppBuilder UseDiagnosticsToolkit(this MauiAppBuilder builder)
    {
        builder.Services.AddRuntimeDiagnostics();
        return builder;
    }
}
