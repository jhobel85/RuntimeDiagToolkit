using DiagnosticsToolkit.DependencyInjection;
using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DiagnosticsToolkit.AspNetCore.Extensions;

public static class RuntimeDiagnosticsExtensions
{
    /// <summary>
    /// Adds DiagnosticsToolkit services to the ASP.NET Core DI container.
    /// </summary>
    public static IServiceCollection AddDiagnosticsToolkit(this IServiceCollection services)
    {
        return services.AddRuntimeDiagnostics();
    }

    /// <summary>
    /// Maps a minimal API endpoint that returns runtime metrics as JSON.
    /// </summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <param name="route">Route path. Defaults to "/_diagnostics/runtime".</param>
    public static IEndpointConventionBuilder MapRuntimeDiagnostics(this IEndpointRouteBuilder endpoints, string route = "/_diagnostics/runtime")
    {
        return endpoints.MapGet(route, async (IRuntimeMetricsProvider provider, CancellationToken ct) =>
        {
            var cpu = await provider.GetCpuUsageAsync(ct);
            var mem = await provider.GetMemorySnapshotAsync(ct);
            var gc = await provider.GetGcStatsAsync(ct);
            var tp = await provider.GetThreadPoolStatsAsync(ct);

            return Results.Json(new
            {
                cpu,
                memory = mem,
                gc,
                threadPool = tp
            });
        })
        .WithName("RuntimeDiagnostics");
    }
}
